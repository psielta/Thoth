using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Common.Models;

public sealed record GeneratedPromptDraftDto(
    PromptTemplateKey TemplateKey,
    Guid LinkedDocumentId,
    Guid WorkingDirectoryId,
    string Title,
    string Content,
    TargetAgent TargetAgent,
    PromptKind Kind);
