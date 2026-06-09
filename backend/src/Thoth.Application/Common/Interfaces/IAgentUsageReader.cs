using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Interfaces;

public interface IAgentUsageReader
{
    Task<AgentUsageDto> ReadAsync(CancellationToken cancellationToken);
}
