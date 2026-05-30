using PromptTasks.Domain.Common;

namespace PromptTasks.Domain.Users;

public sealed class User : Entity
{
    public static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public string DisplayName { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
