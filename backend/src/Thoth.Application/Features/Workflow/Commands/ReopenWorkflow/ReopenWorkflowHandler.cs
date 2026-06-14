using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.Workflow.Commands.ReopenWorkflow;

public sealed class ReopenWorkflowHandler(
    IApplicationDbContext context,
    IWorkflowNotifier workflowNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ReopenWorkflowCommand, WorkflowDto>
{
    public async Task<WorkflowDto> Handle(ReopenWorkflowCommand request, CancellationToken cancellationToken)
    {
        var prompt = WorkflowMutationHelpers.GetOwnedPrompt(context, request.PromptId, currentUser.UserId);
        var workflow = context.PromptWorkflows.FirstOrDefault(item => item.PromptId == prompt.Id)
            ?? throw new NotFoundException("Workflow was not found.");
        WorkflowMutationHelpers.EnsureRowVersion(workflow, request.RowVersion);

        if (workflow.Status != PromptWorkflowStatus.Done)
        {
            throw new ConflictException("Only finished workflows can be reopened.");
        }

        var phases = WorkflowMutationHelpers.LoadPhases(context, workflow.Id);
        var now = dateTimeProvider.UtcNow;
        workflow.Status = PromptWorkflowStatus.Active;
        workflow.UpdatedAtUtc = now;

        PromptWorkflowPhase? eventPhase;
        if (request.PhaseId is { } phaseId)
        {
            var target = phases.FirstOrDefault(phase => phase.Id == phaseId)
                ?? throw new NotFoundException("Phase was not found.");
            WorkflowMutationHelpers.EnterPhase(workflow, target, target.DefaultActor, now);
            eventPhase = target;
        }
        else
        {
            eventPhase = phases.FirstOrDefault(phase => phase.Id == workflow.CurrentPhaseId);
        }

        WorkflowMutationHelpers.AppendEvent(
            context, workflow, WorkflowEventType.Reopened, eventPhase, workflow.CurrentActor, null, now);

        await PromptMutationHelpers.ResetBoardRankAsync(context, prompt.Id, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var events = WorkflowMutationHelpers.LoadEvents(context, workflow.Id);
        var dto = workflow.ToDto(phases, events);
        await workflowNotifier.TaskWorkflowChangedAsync(TaskSummaryFactory.Build(context, prompt, workflow), cancellationToken);
        return dto;
    }
}
