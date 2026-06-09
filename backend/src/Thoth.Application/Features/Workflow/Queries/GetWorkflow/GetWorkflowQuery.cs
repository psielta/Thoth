using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Queries.GetWorkflow;

public sealed record GetWorkflowQuery(Guid PromptId) : IRequest<WorkflowDto?>;
