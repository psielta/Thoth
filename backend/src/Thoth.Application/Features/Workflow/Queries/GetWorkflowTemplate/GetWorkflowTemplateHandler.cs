using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Workflow.Queries.GetWorkflowTemplate;

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
