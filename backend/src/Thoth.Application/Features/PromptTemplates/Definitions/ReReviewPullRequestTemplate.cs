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
            $"Re-review {pullRequestReference}: {context.DisplayName}",
            $"""
            /review

            Re-review the {pullRequestReference} after Codex made fixes for the previous review findings.

            The PR implements the plan `{context.AbsolutePath}`. Use the plan as the source of truth, use the current Claude Code session context for the first review when available, and verify that the fixes were actually applied without introducing regressions.

            Codex response after applying fixes:

            ```md
            {codexResponse}
            ```

            Treat the Codex response as a handoff, not proof. Prioritize unresolved bugs, behavioral risks, regressions, and missing tests. Report findings with severity and concrete file/line references when possible. If the PR is now acceptable, say that clearly.
            """));
    }
}
