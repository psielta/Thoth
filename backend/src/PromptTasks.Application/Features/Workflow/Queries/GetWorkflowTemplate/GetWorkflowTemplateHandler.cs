using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow.Queries.GetWorkflowTemplate;

public sealed class GetWorkflowTemplateHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetWorkflowTemplateQuery, WorkflowTemplateDto>
{
    public async Task<WorkflowTemplateDto> Handle(GetWorkflowTemplateQuery request, CancellationToken cancellationToken)
    {
        var (template, phases, created) = WorkflowTemplateHelpers.ResolveOrCreate(context, currentUser.UserId);
        if (created)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return new WorkflowTemplateDto(
            template.Id,
            template.Name,
            phases.OrderBy(phase => phase.OrderIndex).Select(phase => phase.ToDto()).ToList());
    }
}
