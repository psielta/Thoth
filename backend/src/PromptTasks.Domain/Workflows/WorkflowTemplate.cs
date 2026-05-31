using PromptTasks.Domain.Common;

namespace PromptTasks.Domain.Workflows;

public sealed class WorkflowTemplate : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ICollection<WorkflowTemplatePhase> Phases { get; } = new List<WorkflowTemplatePhase>();
}
