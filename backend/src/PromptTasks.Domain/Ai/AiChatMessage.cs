using PromptTasks.Domain.Common;

namespace PromptTasks.Domain.Ai;

public sealed class AiChatMessage : Entity
{
    public Guid SessionId { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = "";
    public int Sequence { get; set; }
    public int? PromptTokens { get; set; }
    public int? CandidateTokens { get; set; }
    public int? CachedTokens { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public AiChatSession Session { get; set; } = null!;
}
