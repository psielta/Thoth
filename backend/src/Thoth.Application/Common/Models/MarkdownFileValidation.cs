namespace Thoth.Application.Common.Models;

public sealed record MarkdownFileValidation(
    bool IsValid,
    string? CanonicalPath,
    string? PathKey,
    long? SizeBytes,
    string? Error)
{
    public static MarkdownFileValidation Valid(string canonicalPath, string pathKey, long sizeBytes) =>
        new(true, canonicalPath, pathKey, sizeBytes, null);

    public static MarkdownFileValidation Invalid(string error) =>
        new(false, null, null, null, error);
}
