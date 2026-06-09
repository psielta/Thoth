using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Commands.CompleteWorkflow;

public sealed record CompleteWorkflowCommand(Guid PromptId, string? Note, string RowVersion) : IRequest<WorkflowDto>;
