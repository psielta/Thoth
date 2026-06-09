using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.Workflow.Commands.ChangeActor;

public sealed class ChangeActorHandler(
    IApplicationDbContext context,
    IWorkflowNotifier workflowNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ChangeActorCommand, WorkflowDto>
{
    public async Task<WorkflowDto> Handle(ChangeActorCommand request, CancellationToken cancellationToken)
    {
        var prompt = WorkflowMutationHelpers.GetOwnedPrompt(context, request.PromptId, currentUser.UserId);
        var workflow = context.PromptWorkflows.FirstOrDefault(item => item.PromptId == prompt.Id)
            ?? throw new NotFoundException("Workflow was not found.");
        WorkflowMutationHelpers.EnsureRowVersion(workflow, request.RowVersion);
        WorkflowMutationHelpers.EnsureActive(workflow);

        var phases = WorkflowMutationHelpers.LoadPhases(context, workflow.Id);
        var current = phases.FirstOrDefault(phase => phase.Id == workflow.CurrentPhaseId);

        var now = dateTimeProvider.UtcNow;
        workflow.CurrentActor = request.Actor;
        workflow.UpdatedAtUtc = now;
        WorkflowMutationHelpers.AppendEvent(
            context, workflow, WorkflowEventType.ActorChanged, current, request.Actor, NormalizeNote(request.Note), now);

        await context.SaveChangesAsync(cancellationToken);

        var events = WorkflowMutationHelpers.LoadEvents(context, workflow.Id);
        var dto = workflow.ToDto(phases, events);
        await workflowNotifier.TaskWorkflowChangedAsync(TaskSummaryFactory.Build(context, prompt, workflow), cancellationToken);
        return dto;
    }

    private static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
