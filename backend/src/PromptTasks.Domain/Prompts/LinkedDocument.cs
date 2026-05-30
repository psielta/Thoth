using PromptTasks.Domain.Common;
using PromptTasks.Domain.WorkingDirectories;

namespace PromptTasks.Domain.Prompts;

public sealed class LinkedDocument : Entity
{
    public Guid PromptId { get; set; }
    public Guid WorkingDirectoryId { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public LinkedDocumentStatus Status { get; set; } = LinkedDocumentStatus.Draft;
    public string? LastContentHash { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public Prompt? Prompt { get; set; }
    public WorkingDirectory? WorkingDirectory { get; set; }
}
