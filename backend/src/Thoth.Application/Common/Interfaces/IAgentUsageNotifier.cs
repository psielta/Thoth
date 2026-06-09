using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Interfaces;

public interface IAgentUsageNotifier
{
    Task AgentUsageUpdatedAsync(AgentUsageDto usage, CancellationToken cancellationToken);
}
