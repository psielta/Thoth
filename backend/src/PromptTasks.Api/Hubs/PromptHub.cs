using Microsoft.AspNetCore.SignalR;
using PromptTasks.Application.Common.Realtime;

namespace PromptTasks.Api.Hubs;

public sealed class PromptHub : Hub<IPromptClient>
{
    public Task JoinWorkingDirectory(Guid id) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(id));

    public Task LeaveWorkingDirectory(Guid id) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(id));

    public Task JoinTasks() =>
        Groups.AddToGroupAsync(Context.ConnectionId, TasksGroupName);

    public Task LeaveTasks() =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, TasksGroupName);

    public static string GroupName(Guid workingDirectoryId) => $"wd:{workingDirectoryId}";

    public const string TasksGroupName = "tasks:all";
}
