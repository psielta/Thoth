using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.PromptTemplates.Definitions;

public sealed class ImplementPlanInWorktreeTemplate : IPromptTemplateDefinition
{
    public PromptTemplateKey Key => PromptTemplateKey.ImplementPlanInWorktree;
    public string DisplayName => "Implementar em worktree";
    public string Description => "Gera um prompt para implementar o plano em uma worktree separada e abrir PR.";
    public TargetAgent DefaultTargetAgent => TargetAgent.Codex;
    public PromptKind DefaultKind => PromptKind.General;
    public WorkflowPhaseRole? TargetPhaseRole => WorkflowPhaseRole.Implementation;
    public PromptTemplateInputDefinition? Input => null;

    public Task<RenderedPromptTemplate> RenderAsync(
        PromptTemplateContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(new RenderedPromptTemplate(
            $"Implementar em worktree: {context.DisplayName}",
            $"""
            Implemente o plano `{context.AbsolutePath}` completamente em uma worktree separada.

            Preserve o checkout principal e as alterações locais não relacionadas. Ao terminar, rode as validações aplicáveis, deixe o branch pronto para revisão e abra um PR.
            """));
}
