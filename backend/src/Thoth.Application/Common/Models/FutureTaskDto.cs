using Thoth.Domain.FutureTasks;

namespace Thoth.Application.Common.Models;

public sealed record FutureTaskDto(
    Guid Id,
    Guid WorkingDirectoryId,
    string Title,
    string Description,
    FutureTaskStatus Status,
    FutureTaskType Type,
    IReadOnlyList<string> Labels,
    string? IssueGithubId,
    string RowVersion,
    int LinkedPromptCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
