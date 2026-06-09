using Thoth.Domain.Workflows;

namespace Thoth.Application.Common.Models;

public sealed record WorkflowPhaseDto(
    Guid Id,
    string Name,
    WorkflowActor DefaultActor,
    int OrderIndex,
    string Color,
    WorkflowPhaseRole? Role);
