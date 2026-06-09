using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Commands.ReopenWorkflow;

public sealed record ReopenWorkflowCommand(Guid PromptId, Guid? PhaseId, string RowVersion) : IRequest<WorkflowDto>;
