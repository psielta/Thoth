using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Interfaces;

public interface IWorkflowNotifier
{
    Task TaskWorkflowChangedAsync(TaskSummaryDto summary, CancellationToken cancellationToken);
}
