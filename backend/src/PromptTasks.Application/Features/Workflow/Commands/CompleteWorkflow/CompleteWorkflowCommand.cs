using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Commands.CompleteWorkflow;

public sealed record CompleteWorkflowCommand(Guid PromptId, string? Note, string RowVersion) : IRequest<WorkflowDto>;
