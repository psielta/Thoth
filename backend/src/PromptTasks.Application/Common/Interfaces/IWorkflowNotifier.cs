using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Common.Interfaces;

public interface IWorkflowNotifier
{
    Task TaskWorkflowChangedAsync(TaskSummaryDto summary, CancellationToken cancellationToken);
}
