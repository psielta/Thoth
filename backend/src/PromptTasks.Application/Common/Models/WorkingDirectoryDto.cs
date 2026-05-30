namespace PromptTasks.Application.Common.Models;

public sealed record WorkingDirectoryDto(
    Guid Id,
    string Name,
    string AbsolutePath,
    bool RespectGitignore,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
