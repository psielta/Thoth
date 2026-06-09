using Microsoft.AspNetCore.SignalR;
using Thoth.Api.Hubs;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Realtime;

namespace Thoth.Api.Realtime;

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