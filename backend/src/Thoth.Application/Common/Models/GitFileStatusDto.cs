namespace Thoth.Application.Common.Models;

public sealed record GitFileStatusDto(string Path, GitFileChangeStatus Status, string? OriginalPath = null);

public enum GitFileChangeStatus
{
    Modified = 1,
    Added = 2,
    Deleted = 3,
    Renamed = 4,
    Untracked = 5
}
