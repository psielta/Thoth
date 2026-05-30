using Microsoft.AspNetCore.SignalR;
using PromptTasks.Application.Common.Realtime;

namespace PromptTasks.Api.Hubs;

public sealed class PromptHub : Hub<IPromptClient>
{
    public Task JoinWorkingDirectory(Guid id) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(id));

    public Task LeaveWorkingDirectory(Guid id) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(id));

    public static string GroupName(Guid workingDirectoryId) => $"wd:{workingDirectoryId}";
}
