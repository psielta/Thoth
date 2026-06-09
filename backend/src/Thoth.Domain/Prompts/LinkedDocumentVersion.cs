using Thoth.Domain.Common;

namespace Thoth.Domain.Prompts;

public sealed class LinkedDocumentVersion : Entity
{
    public Guid LinkedDocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public LinkedDocumentVersionSource Source { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public LinkedDocument? LinkedDocument { get; set; }
}
