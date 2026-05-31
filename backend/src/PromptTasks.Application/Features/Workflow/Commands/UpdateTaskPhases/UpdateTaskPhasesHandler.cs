using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Workflows;

namespace PromptTasks.Application.Features.Workflow.Commands.UpdateTaskPhases;

public sealed class UpdateTaskPhasesHandler(
    IApplicationDbContext context,
    IWorkflowNotifier workflowNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdateTaskPhasesCommand, WorkflowDto>
{
    public async Task<WorkflowDto> Handle(UpdateTaskPhasesCommand request, CancellationToken cancellationToken)
    {
        var prompt = WorkflowMutationHelpers.GetOwnedPrompt(context, request.PromptId, currentUser.UserId);
        var workflow = context.PromptWorkflows.FirstOrDefault(item => item.PromptId == prompt.Id)
            ?? throw new NotFoundException("Workflow was not found.");
        WorkflowMutationHelpers.EnsureRowVersion(workflow, request.RowVersion);

        var existing = WorkflowMutationHelpers.LoadPhases(context, workflow.Id);
        var existingEvents = WorkflowMutationHelpers.LoadEvents(context, workflow.Id);
        var phaseIdsWithEvents = existingEvents
            .Where(@event => @event.PhaseId.HasValue)
            .Select(@event => @event.PhaseId!.Value)
            .ToHashSet();
        var existingById = existing.ToDictionary(phase => phase.Id);
        var keptIds = request.Phases.Where(phase => phase.Id.HasValue).Select(phase => phase.Id!.Value).ToHashSet();

        foreach (var phase in existing)
        {
            if (keptIds.Contains(phase.Id))
            {
                continue;
            }

            if (phase.Id == workflow.CurrentPhaseId)
            {
                throw new ConflictException("Não é possível excluir a fase atual da tarefa.");
            }

            if (phaseIdsWithEvents.Contains(phase.Id))
            {
                throw new ConflictException("Não é possível excluir uma fase que já tem histórico.");
            }

            context.Remove(phase);
        }

        foreach (var input in request.Phases)
        {
            if (input.Id is { } id && existingById.TryGetValue(id, out var phase))
            {
                phase.Name = input.Name.Trim();
                phase.DefaultActor = input.DefaultActor;
                phase.OrderIndex = input.OrderIndex;
                phase.Color = input.Color;
            }
            else
            {
                context.Add(new PromptWorkflowPhase
                {
                    PromptWorkflowId = workflow.Id,
                    Name = input.Name.Trim(),
                    DefaultActor = input.DefaultActor,
                    OrderIndex = input.OrderIndex,
                    Color = input.Color
                });
            }
        }

        var now = dateTimeProvider.UtcNow;
        if (workflow.CurrentPhaseId is { } currentId && existingById.TryGetValue(currentId, out var currentPhase))
        {
            workflow.CurrentPhaseName = currentPhase.Name;
            workflow.CurrentPhaseColor = currentPhase.Color;
            WorkflowMutationHelpers.AppendEvent(
                context, workflow, WorkflowEventType.PhasesEdited, currentPhase, workflow.CurrentActor, null, now);
        }
        else
        {
            WorkflowMutationHelpers.AppendEvent(
                context, workflow, WorkflowEventType.PhasesEdited, null, workflow.CurrentActor, null, now);
        }

        workflow.UpdatedAtUtc = now;
        await context.SaveChangesAsync(cancellationToken);

        var phases = WorkflowMutationHelpers.LoadPhases(context, workflow.Id);
        var events = WorkflowMutationHelpers.LoadEvents(context, workflow.Id);
        var dto = workflow.ToDto(phases, events);
        await workflowNotifier.TaskWorkflowChangedAsync(TaskSummaryFactory.Build(context, prompt, workflow), cancellationToken);
        return dto;
    }
}
