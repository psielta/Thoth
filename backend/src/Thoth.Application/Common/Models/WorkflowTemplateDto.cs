namespace Thoth.Application.Common.Models;

public sealed record WorkflowTemplateDto(
    Guid Id,
    string Name,
    IReadOnlyList<WorkflowPhaseDto> Phases);
