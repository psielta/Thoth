using PromptTasks.Domain.Workflows;

namespace PromptTasks.Application.Common.Models;

public sealed record WorkflowEventDto(
    Guid Id,
    WorkflowEventType Type,
    Guid? PhaseId,
    string? PhaseName,
    WorkflowActor? Actor,
    string? Note,
    DateTimeOffset OccurredAtUtc);
