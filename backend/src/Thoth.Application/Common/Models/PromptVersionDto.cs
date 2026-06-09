using Thoth.Domain.Prompts;

namespace Thoth.Application.Common.Models;

public sealed record PromptVersionDto(
    Guid Id,
    Guid PromptId,
    int VersionNumber,
    string Title,
    string Content,
    TargetAgent TargetAgent,
    PromptKind Kind,
    PromptStatus Status,
    string? ChangeNote,
    DateTimeOffset CreatedAtUtc);
