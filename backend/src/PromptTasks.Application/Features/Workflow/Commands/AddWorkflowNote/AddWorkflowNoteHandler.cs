using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Workflows;

namespace PromptTasks.Application.Features.Workflow.Commands.AddWorkflowNote;

// Notes are append-only and allowed in any status (including Done). They never change the
// workflow row, so no rowVersion is required and no optimistic-concurrency conflict can occur.
public sealed class AddWorkflowNoteHandler(
    IApplicationDbContext context,
    IWorkflowNotifier workflowNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<AddWorkflowNoteCommand, WorkflowDto>
{
    public async Task<WorkflowDto> Handle(AddWorkflowNoteCommand request, CancellationToken cancellationToken)
    {
        var prompt = WorkflowMutationHelpers.GetOwnedPrompt(context, request.PromptId, currentUser.UserId);
        var workflow = context.PromptWorkflows.FirstOrDefault(item => item.PromptId == prompt.Id)
            ?? throw new NotFoundException("Workflow was not found.");

        var phases = WorkflowMutationHelpers.LoadPhases(context, workflow.Id);
        var current = phases.FirstOrDefault(phase => phase.Id == workflow.CurrentPhaseId);

        WorkflowMutationHelpers.AppendEvent(
            context,
            workflow,
            WorkflowEventType.Note,
            current,
            workflow.CurrentActor,
            request.Note.Trim(),
            dateTimeProvider.UtcNow);

        await context.SaveChangesAsync(cancellationToken);

        var events = WorkflowMutationHelpers.LoadEvents(context, workflow.Id);
        var dto = workflow.ToDto(phases, events);
        await workflowNotifier.TaskWorkflowChangedAsync(TaskSummaryFactory.Build(context, prompt, workflow), cancellationToken);
        return dto;
    }
}
