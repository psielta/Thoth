using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Commands.StartWorkflow;

public sealed record StartWorkflowCommand(Guid PromptId, int? InitialPhaseOrderIndex) : IRequest<WorkflowDto>;
