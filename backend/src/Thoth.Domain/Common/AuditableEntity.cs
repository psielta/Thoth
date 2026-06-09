namespace Thoth.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public Guid OwnerId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
