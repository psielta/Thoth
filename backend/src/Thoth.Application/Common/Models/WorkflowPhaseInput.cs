using Thoth.Domain.Workflows;

namespace Thoth.Application.Common.Models;

public sealed record WorkflowPhaseInput(
    Guid? Id,
    string Name,
    WorkflowActor DefaultActor,
    int OrderIndex,
    string Color);
