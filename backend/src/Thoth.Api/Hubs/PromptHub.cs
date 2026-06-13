using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Thoth.Api.Common;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Realtime;
using Thoth.Infrastructure.FileSystem;
using Thoth.Infrastructure.Terminals;

namespace Thoth.Api.Hubs;

public sealed class PromptHub(
    IWorkspaceFileWatchCoordinator workspaceFileWatchCoordinator,
    ITerminalSessionCoordinator terminalCoordinator,
    IOptions<TerminalOptions> terminalOptions) : Hub<IPromptClient>
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

    public async Task JoinTerminal(Guid sessionId)
    {
        TerminalAccessGuard.EnsureHubAccess(terminalOptions, Context.GetHttpContext()?.Connection.RemoteIpAddress);
        await Groups.AddToGroupAsync(Context.ConnectionId, TerminalGroupName(sessionId));
        terminalCoordinator.AttachConnection(sessionId, Context.ConnectionId);
    }

    public async Task LeaveTerminal(Guid sessionId)
    {
        TerminalAccessGuard.EnsureHubAccess(terminalOptions, Context.GetHttpContext()?.Connection.RemoteIpAddress);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TerminalGroupName(sessionId));
        terminalCoordinator.DetachConnection(sessionId, Context.ConnectionId);
    }

    public Task SendTerminalInput(Guid sessionId, string dataBase64)
    {
        TerminalAccessGuard.EnsureHubAccess(terminalOptions, Context.GetHttpContext()?.Connection.RemoteIpAddress);
        var bytes = Convert.FromBase64String(dataBase64);
        terminalCoordinator.WriteInput(sessionId, bytes);
        return Task.CompletedTask;
    }

    public Task ResizeTerminal(Guid sessionId, ushort cols, ushort rows)
    {
        TerminalAccessGuard.EnsureHubAccess(terminalOptions, Context.GetHttpContext()?.Connection.RemoteIpAddress);
        terminalCoordinator.Resize(sessionId, cols, rows);
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        workspaceFileWatchCoordinator.ReleaseConnection(Context.ConnectionId);
        terminalCoordinator.ReleaseConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public static string GroupName(Guid workingDirectoryId) => $"wd:{workingDirectoryId}";

    public static string FileGroupName(Guid workingDirectoryId, string relativePath) =>
        $"file:{workingDirectoryId}:{WorkspaceFilePath.CreateFileKey(relativePath)}";

    public static string TerminalGroupName(Guid sessionId) => $"terminal:{sessionId}";

    public const string TasksGroupName = "tasks:all";
}