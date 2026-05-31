using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Commands.StartWorkflow;

public sealed record StartWorkflowCommand(Guid PromptId, int? InitialPhaseOrderIndex) : IRequest<WorkflowDto>;
