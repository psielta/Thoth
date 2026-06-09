using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Interfaces;

public interface ICodexUsageReader
{
    Task<AgentUsageInfo> ReadAsync(CancellationToken cancellationToken);
}
