using Thoth.Domain.Common;

namespace Thoth.Domain.FutureTasks;

public sealed class FutureTaskLabel : Entity
{
    public Guid FutureTaskId { get; set; }
    public string Label { get; set; } = string.Empty;

    public FutureTask? FutureTask { get; set; }
}
