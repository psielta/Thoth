using Thoth.Application.Common.Interfaces;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.Workflow;

internal static class WorkflowTemplateHelpers
{
    /// <summary>
    /// Returns the owner's default template and its ordered phases, creating (and queueing for save)
    /// a default template when the owner has none yet. Caller is responsible for SaveChangesAsync.
    /// </summary>
    public static (WorkflowTemplate Template, List<WorkflowTemplatePhase> Phases, bool Created) ResolveOrCreate(
        IApplicationDbContext context,
        Guid ownerId)
    {
        var template = context.WorkflowTemplates.FirstOrDefault(item => item.OwnerId == ownerId);
        if (template is null)
        {
            template = WorkflowDefaults.BuildTemplate(ownerId);
            context.Add(template);
            var createdPhases = template.Phases.OrderBy(phase => phase.OrderIndex).ToList();
            foreach (var phase in createdPhases)
            {
                context.Add(phase);
            }

            return (template, createdPhases, true);
        }

        var phases = context.WorkflowTemplatePhases
            .Where(phase => phase.WorkflowTemplateId == template.Id)
            .OrderBy(phase => phase.OrderIndex)
            .ToList();

        foreach (var phase in phases.Where(phase => !phase.Role.HasValue))
        {
            phase.Role = WorkflowDefaults.ResolveRoleByName(phase.Name);
        }

        return (template, phases, false);
    }
}
