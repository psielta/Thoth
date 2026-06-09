using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Queries.GetWorkflowTemplate;

public sealed record GetWorkflowTemplateQuery : IRequest<WorkflowTemplateDto>;
