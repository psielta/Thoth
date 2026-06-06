using Microsoft.AspNetCore.SignalR;
using PromptTasks.Api.Hubs;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Realtime;

namespace PromptTasks.Api.Realtime;

public sealed class SignalRWorkspaceFileNotifier(IHubContext<PromptHub, IPromptClient> hubContext) : IWorkspaceFileNotifier
{
    public Task WorkspaceFileChangedAsync(
        Guid workingDirectoryId,
        string relativePath,
        CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(PromptHub.FileGroupName(workingDirectoryId, relativePath))
            .WorkspaceFileChanged(workingDirectoryId, relativePath);
}