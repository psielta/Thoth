using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Workflows;

namespace PromptTasks.Application.Features.Workflow.Commands.SetPhase;

public sealed record SetPhaseCommand(
    Guid PromptId,
    Guid PhaseId,
    WorkflowActor? Actor,
    string? Note,
    string RowVersion) : IRequest<WorkflowDto>;
