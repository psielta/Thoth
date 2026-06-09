using System.Globalization;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.Workflow;

internal static class WorkflowMutationHelpers
{
    public static Prompt GetOwnedPrompt(IApplicationDbContext context, Guid promptId, Guid ownerId)
    {
        var prompt = context.Prompts.FirstOrDefault(item => item.Id == promptId && item.OwnerId == ownerId);
        return prompt ?? throw new NotFoundException("Prompt was not found.");
    }

    public static PromptWorkflow GetWorkflow(IApplicationDbContext context, Guid promptId, Guid ownerId)
    {
        // Ownership is enforced through the prompt; never reveal existence to other owners.
        _ = GetOwnedPrompt(context, promptId, ownerId);

        var workflow = context.PromptWorkflows.FirstOrDefault(item => item.PromptId == promptId);
        return workflow ?? throw new NotFoundException("Workflow was not found.");
    }

    public static List<PromptWorkflowPhase> LoadPhases(IApplicationDbContext context, Guid workflowId) =>
        context.PromptWorkflowPhases
            .Where(phase => phase.PromptWorkflowId == workflowId)
            .OrderBy(phase => phase.OrderIndex)
            .ToList();

    public static List<PromptWorkflowEvent> LoadEvents(IApplicationDbContext context, Guid workflowId) =>
        context.PromptWorkflowEvents
            .Where(@event => @event.PromptWorkflowId == workflowId)
            .OrderBy(@event => @event.OccurredAtUtc)
            .ToList();

    public static void EnsureRowVersion(PromptWorkflow workflow, string rowVersion)
    {
        if (!uint.TryParse(rowVersion, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ||
            parsed != workflow.RowVersion)
        {
            throw new ConflictException("The workflow was changed by another operation. Reload it before saving.");
        }
    }

    public static void EnsureActive(PromptWorkflow workflow)
    {
        if (workflow.Status != PromptWorkflowStatus.Active)
        {
            throw new ConflictException("The workflow is finished. Reopen it before changing phases.");
        }
    }

    public static PromptWorkflowEvent AppendEvent(
        IApplicationDbContext context,
        PromptWorkflow workflow,
        WorkflowEventType type,
        PromptWorkflowPhase? phase,
        WorkflowActor? actor,
        string? note,
        DateTimeOffset now)
    {
        var @event = new PromptWorkflowEvent
        {
            PromptWorkflowId = workflow.Id,
            Type = type,
            PhaseId = phase?.Id,
            PhaseNameSnapshot = phase?.Name,
            Actor = actor,
            Note = note,
            OccurredAtUtc = now
        };

        context.Add(@event);
        return @event;
    }

    public static void EnterPhase(
        PromptWorkflow workflow,
        PromptWorkflowPhase phase,
        WorkflowActor? actor,
        DateTimeOffset now,
        int iteration = 1)
    {
        workflow.CurrentPhaseId = phase.Id;
        workflow.CurrentPhaseName = phase.Name;
        workflow.CurrentPhaseColor = phase.Color;
        workflow.CurrentActor = actor ?? phase.DefaultActor;
        workflow.CurrentPhaseIteration = iteration;
        // O contexto de veredito vale apenas para a fase em que foi lancado; qualquer transicao o limpa.
        // O AddReviewVerdictHandler redefine este campo logo apos entrar na fase de correcao.
        workflow.ReviewVerdictSourcePhaseName = null;
        workflow.EnteredCurrentPhaseAtUtc = now;
        workflow.UpdatedAtUtc = now;
    }
}
