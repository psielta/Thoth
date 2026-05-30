namespace PromptTasks.Application.Common.Models;

public sealed record LinkedDocumentContentDto(
    Guid LinkedDocumentId,
    int VersionNumber,
    string Content,
    string ContentHash,
    long SizeBytes,
    DateTimeOffset CreatedAtUtc);
