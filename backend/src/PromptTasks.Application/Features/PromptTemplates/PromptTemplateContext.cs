namespace PromptTasks.Application.Features.PromptTemplates;

public sealed record PromptTemplateContext(
    string AbsolutePath,
    string DisplayName,
    Func<CancellationToken, Task<string?>>? PlanContentLoader = null);
