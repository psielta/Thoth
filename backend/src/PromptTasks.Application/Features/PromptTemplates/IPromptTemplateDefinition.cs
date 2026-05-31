using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.PromptTemplates;

public interface IPromptTemplateDefinition
{
    PromptTemplateKey Key { get; }
    string DisplayName { get; }
    string Description { get; }
    TargetAgent DefaultTargetAgent { get; }
    PromptKind DefaultKind { get; }

    Task<RenderedPromptTemplate> RenderAsync(
        PromptTemplateContext context,
        CancellationToken cancellationToken);
}

public sealed record RenderedPromptTemplate(string Title, string Content);
