using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Commands.AdvancePhase;

public sealed record AdvancePhaseCommand(Guid PromptId, string RowVersion, string? Note) : IRequest<WorkflowDto>;
