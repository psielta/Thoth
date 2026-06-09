using Thoth.Domain.Common;

namespace Thoth.Domain.Ai;

public sealed class AiUserSettings : AuditableEntity
{
    public string Model { get; set; } = "gemini-3.5-flash";
    public double Temperature { get; set; } = 0.7;
    public bool ThinkingEnabled { get; set; } = true;
    public int? ThinkingBudget { get; set; }
    public string? ThinkingLevel { get; set; } = "high";
}
