using Microsoft.AspNetCore.SignalR;
using PromptTasks.Api.Hubs;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Common.Realtime;

namespace PromptTasks.Api.Realtime;

public sealed class SignalRWorkflowNotifier(IHubContext<PromptHub, IPromptClient> hubContext) : IWorkflowNotifier
{
    public Task TaskWorkflowChangedAsync(TaskSummaryDto summary, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Groups(PromptHub.GroupName(summary.WorkingDirectoryId), PromptHub.TasksGroupName)
            .TaskWorkflowChanged(summary);
}
