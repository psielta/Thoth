using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.PromptTemplates.Definitions;

public sealed class MergePullRequestTemplate : IPromptTemplateDefinition
{
    public PromptTemplateKey Key => PromptTemplateKey.MergePullRequest;
    public string DisplayName => "Fazer merge da PR";
    public string Description => "Gera um prompt para o Codex fazer merge seguro da PR.";
    public TargetAgent DefaultTargetAgent => TargetAgent.Codex;
    public PromptKind DefaultKind => PromptKind.General;
    public WorkflowPhaseRole? TargetPhaseRole => WorkflowPhaseRole.Merge;
    public PromptTemplateInputDefinition? Input => new(
        "pullRequest",
        "PR",
        "#123 ou URL da PR",
        "Informe o numero ou link da PR que deve ser mesclada.");

    public Task<RenderedPromptTemplate> RenderAsync(
        PromptTemplateContext context,
        CancellationToken cancellationToken)
    {
        var pullRequestReference = PullRequestTemplateHelpers.FormatPullRequestReference(context.PullRequestReference);

        return Task.FromResult(new RenderedPromptTemplate(
            $"Mesclar {pullRequestReference}: {context.DisplayName}",
            $"""
            Faça o merge do {pullRequestReference} que implementa o plano `{context.AbsolutePath}`.

            Antes de mesclar, confirme que o PR está pronto para merge, que as validações necessárias passaram e preserve as alterações locais não relacionadas.

            Se houver conflitos ou checks falhando, pare e reporte o bloqueio exato. Após o merge, sincronize o branch main local com o remoto, remova a worktree se existir, exclua o branch local/remoto se ainda existirem e for seguro, e confirme o estado final do repositório.
            """));
    }
}
