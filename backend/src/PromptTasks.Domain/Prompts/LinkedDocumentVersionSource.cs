namespace PromptTasks.Domain.Prompts;

public enum LinkedDocumentVersionSource
{
    Initial = 1,
    FileChanged = 2,
    ManualRefresh = 3,
    Resumed = 4
}
