using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Workflows;

namespace PromptTasks.Application.Features.Workflow.Commands.AddReviewVerdict;

// Registra o veredito de uma revisao (plano ou codigo) como nota e, na mesma operacao, move o
// fluxo para a fase de correcao correspondente. O card passa a indicar a fase de origem do veredito.
//   Revisao do plano  -> Correcao do plano
//   Revisao de codigo -> Correcao da revisao
public sealed class AddReviewVerdictHandler(
    IApplicationDbContext context,
    IWorkflowNotifier workflowNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<AddReviewVerdictCommand, WorkflowDto>
{
    public async Task<WorkflowDto> Handle(AddReviewVerdictCommand request, CancellationToken cancellationToken)
    {
        var prompt = WorkflowMutationHelpers.GetOwnedPrompt(context, request.PromptId, currentUser.UserId);
        var workflow = context.PromptWorkflows.FirstOrDefault(item => item.PromptId == prompt.Id)
            ?? throw new NotFoundException("Workflow was not found.");
        WorkflowMutationHelpers.EnsureRowVersion(workflow, request.RowVersion);
        WorkflowMutationHelpers.EnsureActive(workflow);

        var phases = WorkflowMutationHelpers.LoadPhases(context, workflow.Id);
        var current = phases.FirstOrDefault(phase => phase.Id == workflow.CurrentPhaseId)
            ?? throw new ConflictException("A fase atual do fluxo nao foi encontrada.");

        var currentRole = current.Role ?? WorkflowDefaults.ResolveRoleByName(current.Name);
        var targetRole = ResolveTargetRole(currentRole)
            ?? throw new ConflictException("So e possivel lancar veredito em uma fase de revisao (plano ou codigo).");

        var target = phases.FirstOrDefault(phase =>
                (phase.Role ?? WorkflowDefaults.ResolveRoleByName(phase.Name)) == targetRole)
            ?? throw new ConflictException("Nao ha fase de correcao correspondente no fluxo desta tarefa.");
        target.Role ??= targetRole;

        var now = dateTimeProvider.UtcNow;
        var reviewPhaseName = current.Name;
        var reviewerActor = workflow.CurrentActor;

        // 1) Veredito como nota, atribuido ao revisor e ancorado na fase de revisao (antes de trocar de fase).
        WorkflowMutationHelpers.AppendEvent(
            context, workflow, WorkflowEventType.Note, current, reviewerActor, request.Verdict.Trim(), now);

        // 2) Move para a fase de correcao. EnterPhase limpa ReviewVerdictSourcePhaseName.
        WorkflowMutationHelpers.EnterPhase(workflow, target, target.DefaultActor, now);
        WorkflowMutationHelpers.AppendEvent(
            context,
            workflow,
            WorkflowEventType.PhaseChanged,
            target,
            target.DefaultActor,
            $"Trabalhando no veredito de \"{reviewPhaseName}\".",
            now);

        // 3) Marca a fase de origem para o card indicar que a correcao trata aquele veredito.
        workflow.ReviewVerdictSourcePhaseName = reviewPhaseName;

        await context.SaveChangesAsync(cancellationToken);

        var events = WorkflowMutationHelpers.LoadEvents(context, workflow.Id);
        var dto = workflow.ToDto(phases, events);
        await workflowNotifier.TaskWorkflowChangedAsync(TaskSummaryFactory.Build(context, prompt, workflow), cancellationToken);
        return dto;
    }

    private static WorkflowPhaseRole? ResolveTargetRole(WorkflowPhaseRole? reviewRole) => reviewRole switch
    {
        WorkflowPhaseRole.PlanReview => WorkflowPhaseRole.PlanCorrection,
        WorkflowPhaseRole.CodeReview => WorkflowPhaseRole.ReviewCorrection,
        _ => null,
    };
}
