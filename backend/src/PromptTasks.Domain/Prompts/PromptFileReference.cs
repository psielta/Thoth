using PromptTasks.Domain.Common;

namespace PromptTasks.Domain.Prompts;

public sealed class PromptFileReference : Entity
{
    public Guid PromptId { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string RawMention { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }

    public Prompt? Prompt { get; set; }
}
