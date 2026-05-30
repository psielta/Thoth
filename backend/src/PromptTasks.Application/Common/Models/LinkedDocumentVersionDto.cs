using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Common.Models;

public sealed record LinkedDocumentVersionDto(
    Guid Id,
    Guid LinkedDocumentId,
    int VersionNumber,
    string ContentHash,
    long SizeBytes,
    LinkedDocumentVersionSource Source,
    DateTimeOffset CreatedAtUtc);
