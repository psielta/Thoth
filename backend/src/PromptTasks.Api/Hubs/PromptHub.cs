using Microsoft.AspNetCore.SignalR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Realtime;
using PromptTasks.Infrastructure.FileSystem;

namespace PromptTasks.Api.Hubs;

public sealed class PromptHub(IWorkspaceFileWatchCoordinator workspaceFileWatchCoordinator) : Hub<IPromptClient>
{
    public Task JoinWorkingDirectory(Guid id) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(id));

    public Task LeaveWorkingDirectory(Guid id) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(id));

    public Task JoinTasks() =>
        Groups.AddToGroupAsync(Context.ConnectionId, TasksGroupName);

    public Task LeaveTasks() =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, TasksGroupName);

    public async Task JoinFile(Guid workingDirectoryId, string relativePath)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, FileGroupName(workingDirectoryId, relativePath));
        await workspaceFileWatchCoordinator.JoinFileAsync(
            workingDirectoryId,
            relativePath,
            Context.ConnectionId,
            Context.ConnectionAborted);
    }

    public async Task LeaveFile(Guid workingDirectoryId, string relativePath)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, FileGroupName(workingDirectoryId, relativePath));
        await workspaceFileWatchCoordinator.LeaveFileAsync(
            workingDirectoryId,
            relativePath,
            Context.ConnectionId,
            Context.ConnectionAborted);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        workspaceFileWatchCoordinator.ReleaseConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public static string GroupName(Guid workingDirectoryId) => $"wd:{workingDirectoryId}";

    public static string FileGroupName(Guid workingDirectoryId, string relativePath) =>
        $"file:{workingDirectoryId}:{WorkspaceFilePath.CreateFileKey(relativePath)}";

    public const string TasksGroupName = "tasks:all";
}