using Microsoft.Extensions.Options;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Infrastructure.AgentUsage;

public sealed class AgentUsageReader(
    IClaudeUsageReader claudeUsageReader,
    ICodexUsageReader codexUsageReader,
    IDateTimeProvider dateTimeProvider,
    IOptions<AgentUsageOptions> options)
    : IAgentUsageReader
{
    public async Task<AgentUsageDto> ReadAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return new AgentUsageDto(
                dateTimeProvider.UtcNow,
                AgentUsageInfo.Disabled("Claude"),
                AgentUsageInfo.Disabled("Codex"));
        }

        var claudeTask = ReadSafeAsync("Claude", claudeUsageReader.ReadAsync, cancellationToken);
        var codexTask = ReadSafeAsync("Codex", codexUsageReader.ReadAsync, cancellationToken);

        await Task.WhenAll(claudeTask, codexTask);

        return new AgentUsageDto(dateTimeProvider.UtcNow, claudeTask.Result, codexTask.Result);
    }

    private static async Task<AgentUsageInfo> ReadSafeAsync(
        string agent,
        Func<CancellationToken, Task<AgentUsageInfo>> read,
        CancellationToken cancellationToken)
    {
        try
        {
            return await read(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new AgentUsageInfo(
                agent,
                AgentUsageStatus.Unavailable,
                null,
                AgentUsageText.Sanitize(exception.Message),
                null,
                Array.Empty<AgentUsageWindow>());
        }
    }
}
