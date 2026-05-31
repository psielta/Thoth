using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Commands.UpdateTaskPhases;

public sealed record UpdateTaskPhasesCommand(
    Guid PromptId,
    IReadOnlyList<WorkflowPhaseInput> Phases,
    string RowVersion) : IRequest<WorkflowDto>;
