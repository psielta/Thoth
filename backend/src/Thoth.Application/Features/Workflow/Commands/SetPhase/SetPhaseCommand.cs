using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.Workflow.Commands.SetPhase;

public sealed record SetPhaseCommand(
    Guid PromptId,
    Guid PhaseId,
    WorkflowActor? Actor,
    string? Note,
    string RowVersion) : IRequest<WorkflowDto>;
