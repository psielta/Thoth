using PromptTasks.Domain.Common;

namespace PromptTasks.Domain.Prompts;

public sealed class PromptVersion : Entity
{
    public Guid PromptId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public TargetAgent TargetAgent { get; set; }
    public PromptKind Kind { get; set; }
    public PromptStatus Status { get; set; }
    public string? ChangeNote { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public Prompt? Prompt { get; set; }
}
