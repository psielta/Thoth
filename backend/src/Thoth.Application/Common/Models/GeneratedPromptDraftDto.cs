using Thoth.Domain.Prompts;

namespace Thoth.Application.Common.Models;

public sealed record GeneratedPromptDraftDto(
    PromptTemplateKey TemplateKey,
    Guid LinkedDocumentId,
    Guid WorkingDirectoryId,
    Guid ParentPromptId,
    string Title,
    string Content,
    TargetAgent TargetAgent,
    PromptKind Kind);
