namespace PromptTasks.Application.Common.Models;

public sealed record ValidatedPathResult(bool IsValid, string? CanonicalPath, string? Error)
{
    public static ValidatedPathResult Valid(string canonicalPath) => new(true, canonicalPath, null);
    public static ValidatedPathResult Invalid(string error) => new(false, null, error);
}
