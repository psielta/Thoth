using PromptTasks.Domain.Common;
using PromptTasks.Domain.Prompts;
using PromptTasks.Domain.Users;
using PromptTasks.Domain.WorkingDirectories;

namespace PromptTasks.Domain.FutureTasks;

public sealed class FutureTask : AuditableEntity
{
    public Guid WorkingDirectoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FutureTaskStatus Status { get; set; } = FutureTaskStatus.Open;
    public FutureTaskType Type { get; set; } = FutureTaskType.Task;
    public string? IssueGithubId { get; set; }
    public uint RowVersion { get; private set; }

    public WorkingDirectory? WorkingDirectory { get; set; }
    public User? Owner { get; set; }
    public ICollection<FutureTaskLabel> Labels { get; } = new List<FutureTaskLabel>();
    public ICollection<Prompt> Prompts { get; } = new List<Prompt>();
}
