using Microsoft.AspNetCore.SignalR;
using Thoth.Api.Hubs;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Realtime;

namespace Thoth.Api.Realtime;

public sealed class SignalRTerminalNotifier(IHubContext<PromptHub, IPromptClient> hubContext) : ITerminalNotifier
{
    public Task TerminalOutputAsync(Guid sessionId, long startOffset, string dataBase64, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(PromptHub.TerminalGroupName(sessionId))
            .TerminalOutput(sessionId, startOffset, dataBase64);

    public Task TerminalExitedAsync(Guid sessionId, int exitCode, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(PromptHub.TerminalGroupName(sessionId))
            .TerminalExited(sessionId, exitCode);
}
