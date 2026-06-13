using System.Net;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Thoth.Application.Common.Exceptions;
using Thoth.Infrastructure.Terminals;

namespace Thoth.Api.Common;

public static class TerminalAccessGuard
{
    public static bool IsEnabledFor(IOptions<TerminalOptions> options, IPAddress? remoteIp)
    {
        if (!options.Value.Enabled)
        {
            return false;
        }

        if (options.Value.AllowRemoteConnections)
        {
            return true;
        }

        return remoteIp is not null && IPAddress.IsLoopback(remoteIp);
    }

    public static void EnsureAccess(IOptions<TerminalOptions> options, IPAddress? remoteIp)
    {
        if (!options.Value.Enabled)
        {
            throw new NotFoundException("Terminal sessions are not available.");
        }

        if (!options.Value.AllowRemoteConnections && (remoteIp is null || !IPAddress.IsLoopback(remoteIp)))
        {
            throw new ForbiddenException("Terminal sessions are only available from loopback connections.");
        }
    }

    public static void EnsureHubAccess(IOptions<TerminalOptions> options, IPAddress? remoteIp)
    {
        if (!options.Value.Enabled)
        {
            throw new HubException("Terminal sessions are not available.");
        }

        if (!options.Value.AllowRemoteConnections && (remoteIp is null || !IPAddress.IsLoopback(remoteIp)))
        {
            throw new HubException("Terminal sessions are only available from loopback connections.");
        }
    }
}