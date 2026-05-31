using PromptTasks.Domain.Workflows;

namespace PromptTasks.Application.Common.Models;

public sealed record WorkflowPhaseDto(
    Guid Id,
    string Name,
    WorkflowActor DefaultActor,
    int OrderIndex,
    string Color);
