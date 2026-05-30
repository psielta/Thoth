namespace PromptTasks.Application.Common.Models;

public sealed record FileReferenceResolution(string RelativePath, bool Exists, DateTimeOffset ResolvedAtUtc);
