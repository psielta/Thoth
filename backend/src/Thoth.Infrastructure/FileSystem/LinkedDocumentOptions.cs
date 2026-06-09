namespace Thoth.Infrastructure.FileSystem;

public sealed class LinkedDocumentOptions
{
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public int DebounceMilliseconds { get; set; } = 400;
    public int ReconcileSeconds { get; set; } = 60;
    public bool AllowUncPaths { get; set; }
}
