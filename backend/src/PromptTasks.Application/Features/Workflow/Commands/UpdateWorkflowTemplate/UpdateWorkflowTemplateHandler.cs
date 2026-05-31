using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Workflows;

namespace PromptTasks.Application.Features.Workflow.Commands.UpdateWorkflowTemplate;

public sealed class UpdateWorkflowTemplateHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdateWorkflowTemplateCommand, WorkflowTemplateDto>
{
    public async Task<WorkflowTemplateDto> Handle(UpdateWorkflowTemplateCommand request, CancellationToken cancellationToken)
    {
        var (template, existing, _) = WorkflowTemplateHelpers.ResolveOrCreate(context, currentUser.UserId);
        var existingById = existing.ToDictionary(phase => phase.Id);
        var keptIds = request.Phases.Where(phase => phase.Id.HasValue).Select(phase => phase.Id!.Value).ToHashSet();

        foreach (var phase in existing)
        {
            if (!keptIds.Contains(phase.Id))
            {
                context.Remove(phase);
            }
        }

        foreach (var input in request.Phases)
        {
            if (input.Id is { } id && existingById.TryGetValue(id, out var phase))
            {
                phase.Name = input.Name.Trim();
                phase.DefaultActor = input.DefaultActor;
                phase.OrderIndex = input.OrderIndex;
                phase.Color = input.Color;
            }
            else
            {
                context.Add(new WorkflowTemplatePhase
                {
                    WorkflowTemplateId = template.Id,
                    Name = input.Name.Trim(),
                    DefaultActor = input.DefaultActor,
                    OrderIndex = input.OrderIndex,
                    Color = input.Color
                });
            }
        }

        template.UpdatedAtUtc = dateTimeProvider.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        var phases = context.WorkflowTemplatePhases
            .Where(phase => phase.WorkflowTemplateId == template.Id)
            .OrderBy(phase => phase.OrderIndex)
            .ToList();
        return new WorkflowTemplateDto(template.Id, template.Name, phases.Select(phase => phase.ToDto()).ToList());
    }
}
