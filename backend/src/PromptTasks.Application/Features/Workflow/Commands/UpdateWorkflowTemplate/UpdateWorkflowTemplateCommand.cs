using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Commands.UpdateWorkflowTemplate;

public sealed record UpdateWorkflowTemplateCommand(IReadOnlyList<WorkflowPhaseInput> Phases)
    : IRequest<WorkflowTemplateDto>;
