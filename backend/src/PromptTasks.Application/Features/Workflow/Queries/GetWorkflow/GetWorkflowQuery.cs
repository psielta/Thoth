using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Queries.GetWorkflow;

public sealed record GetWorkflowQuery(Guid PromptId) : IRequest<WorkflowDto?>;
