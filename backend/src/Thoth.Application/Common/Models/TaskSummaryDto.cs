using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Common.Models;

public sealed record TaskSummaryDto(
    Guid PromptId,
    Guid WorkingDirectoryId,
    string WorkingDirectoryName,
    string? TaskNumber,
    string Title,
    PromptStatus PromptStatus,
    PromptWorkflowStatus? WorkflowStatus,
    Guid? CurrentPhaseId,
    string? CurrentPhaseName,
    string? CurrentPhaseColor,
    WorkflowActor? CurrentActor,
    DateTimeOffset? EnteredCurrentPhaseAtUtc,
    int CurrentPhaseIteration,
    string? ReviewVerdictSourcePhaseName,
    DateTimeOffset UpdatedAtUtc,
    bool HasChildPrompts,
    bool HasLinkedPlan,
    Guid? LinkedDocumentId,
    string? PullRequestReference,
    string PromptRowVersion,
    IReadOnlyList<WorkflowPhaseDto> Phases,
    string? RowVersion);
