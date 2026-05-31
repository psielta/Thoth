namespace PromptTasks.Domain.Workflows;

public enum WorkflowEventType
{
    WorkflowStarted = 1,
    PhaseChanged = 2,
    ActorChanged = 3,
    Note = 4,
    Completed = 5,
    Reopened = 6,
    PhasesEdited = 7
}
