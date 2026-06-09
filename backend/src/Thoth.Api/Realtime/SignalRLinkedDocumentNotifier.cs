using Microsoft.AspNetCore.SignalR;
using Thoth.Api.Hubs;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Common.Realtime;

namespace Thoth.Api.Realtime;

public sealed class SignalRLinkedDocumentNotifier(IHubContext<PromptHub, IPromptClient> hubContext) : ILinkedDocumentNotifier
{
    public Task LinkedDocumentLinkedAsync(
        LinkedDocumentDto document,
        Guid workingDirectoryId,
        CancellationToken cancellationToken) =>
        hubContext.Clients.Group(PromptHub.GroupName(workingDirectoryId)).LinkedDocumentLinked(document);

    public Task LinkedDocumentUpdatedAsync(
        LinkedDocumentDto document,
        Guid workingDirectoryId,
        CancellationToken cancellationToken) =>
        hubContext.Clients.Group(PromptHub.GroupName(workingDirectoryId)).LinkedDocumentUpdated(document);

    public Task LinkedDocumentRemovedAsync(
        Guid linkedDocumentId,
        Guid promptId,
        Guid workingDirectoryId,
        CancellationToken cancellationToken) =>
        hubContext.Clients.Group(PromptHub.GroupName(workingDirectoryId))
            .LinkedDocumentRemoved(linkedDocumentId, promptId, workingDirectoryId);
}
