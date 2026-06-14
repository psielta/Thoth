using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.PromptTemplates.Definitions;

public sealed class RebaseCurrentBranchTemplate : IPromptTemplateDefinition
{
    public PromptTemplateKey Key => PromptTemplateKey.RebaseCurrentBranch;
    public string DisplayName => "Atualizar branch com main";
    public string Description => "Gera um prompt para atualizar a branch ou worktree atual com as ultimas alteracoes da main remota usando rebase.";
    public TargetAgent DefaultTargetAgent => TargetAgent.Codex;
    public PromptKind DefaultKind => PromptKind.General;
    public WorkflowPhaseRole? TargetPhaseRole => WorkflowPhaseRole.Rebase;
    public PromptTemplateInputDefinition? Input => null;

    public Task<RenderedPromptTemplate> RenderAsync(
        PromptTemplateContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(new RenderedPromptTemplate(
            $"Atualizar branch com main: {context.DisplayName}",
            """
            Atualize meu branch/worktree atual com as últimas alterações do branch main remoto usando rebase.

            Preserve as alterações locais não relacionadas. Se houver conflitos, pare e me avise para resolvermos juntos.
            """));
}
