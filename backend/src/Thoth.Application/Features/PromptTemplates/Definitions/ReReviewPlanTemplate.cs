using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.PromptTemplates.Definitions;

public sealed class ReReviewPlanTemplate : IPromptTemplateDefinition
{
    public PromptTemplateKey Key => PromptTemplateKey.ReReviewPlan;
    public string DisplayName => "Re-review do plano";
    public string Description => "Gera um prompt para revalidar um plano apos Claude corrigir pontos apontados anteriormente.";
    public TargetAgent DefaultTargetAgent => TargetAgent.Codex;
    public PromptKind DefaultKind => PromptKind.Planning;
    public WorkflowPhaseRole? TargetPhaseRole => WorkflowPhaseRole.PlanReview;
    public bool IsReReview => true;
    public PromptTemplateInputDefinition? Input => null;

    public Task<RenderedPromptTemplate> RenderAsync(
        PromptTemplateContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(new RenderedPromptTemplate(
            $"Revisar plano novamente: {context.DisplayName}",
            $"Passei os pontos anteriores para o Claude corrigir no plano \"{context.AbsolutePath}\". Valide o plano atualizado novamente, aprove-o se estiver correto ou aponte as melhorias que ainda faltam."));
}
