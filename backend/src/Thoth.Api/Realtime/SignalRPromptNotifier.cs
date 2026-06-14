using Microsoft.AspNetCore.SignalR;
using Thoth.Api.Hubs;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Common.Realtime;

namespace Thoth.Api.Realtime;

public sealed class SignalRPromptNotifier(IHubContext<PromptHub, IPromptClient> hubContext) : IPromptNotifier
{
    public Task PromptCreatedAsync(PromptDto prompt, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(PromptHub.GroupName(prompt.WorkingDirectoryId)).PromptCreated(prompt);

    public Task PromptUpdatedAsync(PromptDto prompt, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(PromptHub.GroupName(prompt.WorkingDirectoryId)).PromptUpdated(prompt);

    public Task PromptDeletedAsync(Guid promptId, Guid workingDirectoryId, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(PromptHub.GroupName(workingDirectoryId)).PromptDeleted(promptId, workingDirectoryId);

    public Task BoardReorderedAsync(CancellationToken cancellationToken) =>
        hubContext.Clients.Group(PromptHub.TasksGroupName).BoardReordered();
}
