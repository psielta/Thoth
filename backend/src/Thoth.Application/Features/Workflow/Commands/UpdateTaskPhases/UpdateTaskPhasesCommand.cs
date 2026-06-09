using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Commands.UpdateTaskPhases;

public sealed record UpdateTaskPhasesCommand(
    Guid PromptId,
    IReadOnlyList<WorkflowPhaseInput> Phases,
    string RowVersion) : IRequest<WorkflowDto>;
