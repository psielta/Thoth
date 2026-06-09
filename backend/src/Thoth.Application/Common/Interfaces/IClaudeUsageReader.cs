using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Interfaces;

public interface IClaudeUsageReader
{
    Task<AgentUsageInfo> ReadAsync(CancellationToken cancellationToken);
}
