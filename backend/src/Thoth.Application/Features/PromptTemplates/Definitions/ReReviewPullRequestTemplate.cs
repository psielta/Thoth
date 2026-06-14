using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.PromptTemplates.Definitions;

public sealed class ReReviewPullRequestTemplate : IPromptTemplateDefinition
{
    private static readonly PromptTemplateInputDefinition PullRequestInput = new(
        "pullRequest",
        "PR",
        "#123 ou URL da PR",
        "Informe o numero ou link da PR revisada apos as correcoes.");

    private static readonly PromptTemplateInputDefinition CodexResponseInput = new(
        "codexResponse",
        "Resposta do Codex",
        "Cole a resposta do Codex apos corrigir os pontos da primeira revisao",
        "Informe a resposta do Codex depois que ele corrigiu os pontos apontados na primeira revisao.",
        Multiline: true);

    public PromptTemplateKey Key => PromptTemplateKey.ReReviewPullRequest;
    public string DisplayName => "Re-review de PR";
    public string Description => "Gera um prompt para revisar novamente uma PR apos correcoes dos pontos anteriores.";
    public TargetAgent DefaultTargetAgent => TargetAgent.Codex;
    public PromptKind DefaultKind => PromptKind.General;
    public WorkflowPhaseRole? TargetPhaseRole => WorkflowPhaseRole.CodeReview;
    public bool IsReReview => true;
    public PromptTemplateInputDefinition? Input => PullRequestInput;
    public IReadOnlyList<PromptTemplateInputDefinition> Inputs => new[] { PullRequestInput, CodexResponseInput };

    public Task<RenderedPromptTemplate> RenderAsync(
        PromptTemplateContext context,
        CancellationToken cancellationToken)
    {
        var pullRequestReference = PullRequestTemplateHelpers.FormatPullRequestReference(context.PullRequestReference);
        var codexResponse = context.GetInputValue("codexResponse")?.Trim() ?? string.Empty;

        return Task.FromResult(new RenderedPromptTemplate(
            $"Revisar novamente {pullRequestReference}: {context.DisplayName}",
            $"""
            /review

            Revise novamente o {pullRequestReference} depois que o Codex corrigiu os pontos da revisão anterior.

            O PR implementa o plano `{context.AbsolutePath}`. Use o plano como fonte da verdade, use o contexto da sessão atual do Claude Code da primeira revisão quando disponível e verifique se as correções foram realmente aplicadas sem introduzir regressões.

            Resposta do Codex após aplicar as correções:

            ```md
            {codexResponse}
            ```

            Trate a resposta do Codex como um repasse, não como prova. Priorize bugs não resolvidos, riscos de comportamento, regressões e testes ausentes. Reporte os achados com severidade e referências concretas de arquivo/linha quando possível. Se o PR estiver aceitável agora, diga isso claramente.
            """));
    }
}
