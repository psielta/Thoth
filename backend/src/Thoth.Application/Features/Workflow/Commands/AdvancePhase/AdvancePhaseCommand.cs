using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Commands.AdvancePhase;

public sealed record AdvancePhaseCommand(Guid PromptId, string RowVersion, string? Note) : IRequest<WorkflowDto>;
