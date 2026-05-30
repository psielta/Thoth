namespace PromptTasks.Application.Common.Models;

public sealed record MarkdownFileReadResult(
    bool Success,
    string? Content,
    string? ContentHash,
    long? SizeBytes,
    bool FileMissing,
    string? Error)
{
    public static MarkdownFileReadResult Valid(string content, string contentHash, long sizeBytes) =>
        new(true, content, contentHash, sizeBytes, false, null);

    public static MarkdownFileReadResult Missing(string error) =>
        new(false, null, null, null, true, error);

    public static MarkdownFileReadResult Invalid(string error) =>
        new(false, null, null, null, false, error);
}
