using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Prompts;
using Thoth.Application.Features.Terminals;
using Thoth.Application.Features.Workflow;
using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.Terminals.Commands.CreateTerminalSession;

public sealed class CreateTerminalSessionHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    ITerminalSessionCoordinator terminalCoordinator,
    IWorkflowNotifier workflowNotifier,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CreateTerminalSessionCommand, TerminalSessionDescriptor>
{
    public async Task<TerminalSessionDescriptor> Handle(
        CreateTerminalSessionCommand request,
        CancellationToken cancellationToken)
    {
        var prompt = PromptMutationHelpers.GetPrompt(context, request.PromptId, currentUser.UserId);
        if (prompt.Status == PromptStatus.Archived)
        {
            throw new ForbiddenException("Cannot create terminal sessions for archived prompts.");
        }

        var directory = ResolveWorkspaceDirectory(context, prompt, currentUser.UserId);

        var needsContent = request.AgentLaunch == TerminalAgentLaunch.ClaudePlan || request.SubmitPrompt;
        var promptContent = needsContent ? prompt.Content : null;
        var initialInput = TerminalAgentLaunchCommands.ResolveInitialInput(request.AgentLaunch, promptContent);
        var followUpInput = TerminalAgentLaunchCommands.ResolveFollowUpInput(
            request.AgentLaunch,
            promptContent,
            request.SubmitPrompt);

        var descriptor = await terminalCoordinator.CreateAsync(
            prompt.Id,
            directory.AbsolutePath,
            request.Shell ?? string.Empty,
            initialInput,
            cancellationToken,
            followUpInput);

        try
        {
            await TryEnterPlanModeAsync(prompt, request.AgentLaunch, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Opening the terminal is the primary action; workflow sync is best-effort.
        }

        return descriptor;
    }

    private async Task TryEnterPlanModeAsync(
        Prompt prompt,
        TerminalAgentLaunch? agentLaunch,
        CancellationToken cancellationToken)
    {
        if (agentLaunch != TerminalAgentLaunch.ClaudePlan || prompt.ParentPromptId is not null)
        {
            return;
        }

        var workflow = context.PromptWorkflows.FirstOrDefault(item => item.PromptId == prompt.Id);
        if (workflow is null || workflow.Status != PromptWorkflowStatus.Active)
        {
            return;
        }

        var phases = WorkflowMutationHelpers.LoadPhases(context, workflow.Id);
        var planning = phases.FirstOrDefault(phase => phase.Role == WorkflowPhaseRole.Planning)
            ?? phases.FirstOrDefault(phase =>
                !phase.Role.HasValue &&
                WorkflowDefaults.ResolveRoleByName(phase.Name) == WorkflowPhaseRole.Planning);
        if (planning is null)
        {
            return;
        }

        var current = phases.FirstOrDefault(phase => phase.Id == workflow.CurrentPhaseId);
        if (current is not null && current.OrderIndex >= planning.OrderIndex)
        {
            return;
        }

        planning.Role ??= WorkflowPhaseRole.Planning;
        var now = dateTimeProvider.UtcNow;
        WorkflowMutationHelpers.EnterPhase(workflow, planning, planning.DefaultActor, now);
        WorkflowMutationHelpers.AppendEvent(
            context,
            workflow,
            WorkflowEventType.PhaseChanged,
            planning,
            planning.DefaultActor,
            "Plan mode iniciado",
            now);

        await context.SaveChangesAsync(cancellationToken);
        await workflowNotifier.TaskWorkflowChangedAsync(TaskSummaryFactory.Build(context, prompt, workflow), cancellationToken);
    }

    private static Domain.WorkingDirectories.WorkingDirectory ResolveWorkspaceDirectory(
        IApplicationDbContext context,
        Prompt prompt,
        Guid ownerId)
    {
        if (prompt.ParentPromptId is { } parentPromptId)
        {
            var parentPrompt = PromptMutationHelpers.GetPrompt(context, parentPromptId, ownerId);
            return PromptMutationHelpers.GetWorkingDirectory(context, parentPrompt.WorkingDirectoryId, ownerId);
        }

        return PromptMutationHelpers.GetWorkingDirectory(context, prompt.WorkingDirectoryId, ownerId);
    }
}
