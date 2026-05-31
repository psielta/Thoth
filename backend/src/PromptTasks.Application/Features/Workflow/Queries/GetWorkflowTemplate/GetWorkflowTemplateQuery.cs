using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Queries.GetWorkflowTemplate;

public sealed record GetWorkflowTemplateQuery : IRequest<WorkflowTemplateDto>;
