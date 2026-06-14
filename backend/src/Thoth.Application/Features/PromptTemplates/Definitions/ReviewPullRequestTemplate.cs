using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.PromptTemplates.Definitions;

public sealed class ReviewPullRequestTemplate : IPromptTemplateDefinition
{
    public PromptTemplateKey Key => PromptTemplateKey.ReviewPullRequest;
    public string DisplayName => "Revisar PR";
    public string Description => "Gera um prompt de revisao para a PR que implementou o plano.";
    public TargetAgent DefaultTargetAgent => TargetAgent.Codex;
    public PromptKind DefaultKind => PromptKind.General;
    public WorkflowPhaseRole? TargetPhaseRole => WorkflowPhaseRole.CodeReview;
    public PromptTemplateInputDefinition? Input => new(
        "pullRequest",
        "PR",
        "#123 ou URL da PR",
        "Informe o numero ou link da PR criada apos a implementacao do plano.");

    public Task<RenderedPromptTemplate> RenderAsync(
        PromptTemplateContext context,
        CancellationToken cancellationToken)
    {
        var pullRequestReference = PullRequestTemplateHelpers.FormatPullRequestReference(context.PullRequestReference);

        return Task.FromResult(new RenderedPromptTemplate(
            $"Revisar {pullRequestReference}: {context.DisplayName}",
            $"""
            /review

            Revise o {pullRequestReference} que implementa o plano `{context.AbsolutePath}`.

            Use o plano como fonte da verdade. Verifique se o PR implementa o plano completamente, preserva a arquitetura existente, não introduz regressões e se as validações necessárias foram executadas.

            Priorize bugs, riscos de comportamento e testes ausentes. Reporte os achados com severidade e referências concretas de arquivo/linha quando possível.
            """));
    }
}
