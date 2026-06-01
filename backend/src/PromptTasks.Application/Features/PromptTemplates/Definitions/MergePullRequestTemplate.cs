using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.PromptTemplates.Definitions;

public sealed class MergePullRequestTemplate : IPromptTemplateDefinition
{
    public PromptTemplateKey Key => PromptTemplateKey.MergePullRequest;
    public string DisplayName => "Fazer merge da PR";
    public string Description => "Gera um prompt para o Codex fazer merge seguro da PR.";
    public TargetAgent DefaultTargetAgent => TargetAgent.Codex;
    public PromptKind DefaultKind => PromptKind.General;
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
            $"Fazer merge da {pullRequestReference}: {context.DisplayName}",
            $"""
            Faca o merge da {pullRequestReference} que implementa o plano `{context.AbsolutePath}`.

            Antes de mesclar, confirme que a PR esta pronta para merge, que as validacoes necessarias passaram e preserve alteracoes locais nao relacionadas.

            Se houver conflitos ou checks falhando, pare e reporte exatamente o bloqueio. Depois do merge, sincronize a branch principal local com o remoto, remova a worktree se ela existir, exclua a branch local/remota se ainda existirem e for seguro, e confirme o estado final do repositorio.
            """));
    }
}
