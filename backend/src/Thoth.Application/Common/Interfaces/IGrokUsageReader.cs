using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Interfaces;

public interface IGrokUsageReader
{
    Task<AgentUsageInfo> ReadAsync(CancellationToken cancellationToken);
}
