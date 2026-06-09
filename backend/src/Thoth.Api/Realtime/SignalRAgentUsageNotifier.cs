using Microsoft.AspNetCore.SignalR;
using Thoth.Api.Hubs;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Common.Realtime;

namespace Thoth.Api.Realtime;

public sealed class SignalRAgentUsageNotifier(IHubContext<PromptHub, IPromptClient> hubContext) : IAgentUsageNotifier
{
    public Task AgentUsageUpdatedAsync(AgentUsageDto usage, CancellationToken cancellationToken) =>
        hubContext.Clients.All.AgentUsageUpdated(usage);
}
