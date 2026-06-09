using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Commands.UpdateWorkflowTemplate;

public sealed record UpdateWorkflowTemplateCommand(IReadOnlyList<WorkflowPhaseInput> Phases)
    : IRequest<WorkflowTemplateDto>;
