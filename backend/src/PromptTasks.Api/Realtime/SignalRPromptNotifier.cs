using Microsoft.AspNetCore.SignalR;
using PromptTasks.Api.Hubs;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Common.Realtime;

namespace PromptTasks.Api.Realtime;

public sealed class SignalRPromptNotifier(IHubContext<PromptHub, IPromptClient> hubContext) : IPromptNotifier
{
    public Task PromptCreatedAsync(PromptDto prompt, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(PromptHub.GroupName(prompt.WorkingDirectoryId)).PromptCreated(prompt);

    public Task PromptUpdatedAsync(PromptDto prompt, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(PromptHub.GroupName(prompt.WorkingDirectoryId)).PromptUpdated(prompt);

    public Task PromptDeletedAsync(Guid promptId, Guid workingDirectoryId, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(PromptHub.GroupName(workingDirectoryId)).PromptDeleted(promptId, workingDirectoryId);
}
