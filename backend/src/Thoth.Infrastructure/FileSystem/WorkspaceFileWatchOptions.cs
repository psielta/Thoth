namespace Thoth.Infrastructure.FileSystem;

public sealed class WorkspaceFileWatchOptions
{
    public int DebounceMilliseconds { get; set; } = 400;
}