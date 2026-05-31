using PromptTasks.Domain.Common;

namespace PromptTasks.Domain.Workflows;

public sealed class PromptWorkflowEvent : Entity
{
    public Guid PromptWorkflowId { get; set; }
    public WorkflowEventType Type { get; set; }
    public Guid? PhaseId { get; set; }
    public string? PhaseNameSnapshot { get; set; }
    public WorkflowActor? Actor { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }

    public PromptWorkflow? Workflow { get; set; }
}
