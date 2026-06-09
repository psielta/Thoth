using Microsoft.AspNetCore.SignalR;
using Thoth.Api.Hubs;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Common.Realtime;

namespace Thoth.Api.Realtime;

public sealed class SignalRWorkflowNotifier(IHubContext<PromptHub, IPromptClient> hubContext) : IWorkflowNotifier
{
    public Task TaskWorkflowChangedAsync(TaskSummaryDto summary, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Groups(PromptHub.GroupName(summary.WorkingDirectoryId), PromptHub.TasksGroupName)
            .TaskWorkflowChanged(summary);
}
