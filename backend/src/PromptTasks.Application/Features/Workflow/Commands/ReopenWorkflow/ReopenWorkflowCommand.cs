using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Commands.ReopenWorkflow;

public sealed record ReopenWorkflowCommand(Guid PromptId, Guid? PhaseId, string RowVersion) : IRequest<WorkflowDto>;
