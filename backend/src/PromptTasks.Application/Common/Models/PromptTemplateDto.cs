using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Common.Models;

public sealed record PromptTemplateDto(
    PromptTemplateKey Key,
    string DisplayName,
    string Description,
    TargetAgent DefaultTargetAgent,
    PromptKind DefaultKind);
