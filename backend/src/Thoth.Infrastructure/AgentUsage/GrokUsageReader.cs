using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Infrastructure.AgentUsage;

public sealed class GrokUsageReader(
    IOptions<AgentUsageOptions> options,
    IMemoryCache cache,
    IDateTimeProvider dateTimeProvider)
    : IGrokUsageReader
{
    private const string CacheKey = "agent-usage:grok";

    public async Task<AgentUsageInfo> ReadAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out AgentUsageInfo? cached) && cached is not null)
        {
            return cached;
        }

        var result = await ReadFreshAsync(cancellationToken);
        cache.Set(CacheKey, result, TimeSpan.FromSeconds(Math.Max(options.Value.Grok.CacheTtlSeconds, 1)));
        return result;
    }

    private async Task<AgentUsageInfo> ReadFreshAsync(CancellationToken cancellationToken)
    {
        var opts = options.Value.Grok;
        if (opts.FiveHourTokens <= 0 || opts.WeeklyTokens <= 0)
        {
            return new AgentUsageInfo("Grok", AgentUsageStatus.NoData, null, "No budget configured.", null, Array.Empty<AgentUsageWindow>());
        }

        var logsDir = ResolveLogsDir();
        if (logsDir is null || !Directory.Exists(logsDir))
        {
            return new AgentUsageInfo("Grok", AgentUsageStatus.NoData, null, "Grok logs directory was not found.", null, Array.Empty<AgentUsageWindow>());
        }

        try
        {
            var now = dateTimeProvider.UtcNow;
            var fiveHourStart = now.AddHours(-5);
            var weeklyStart = now.AddDays(-7);
            long fiveHourTokens = 0;
            long weeklyTokens = 0;
            DateTimeOffset? oldestInFiveHour = null;
            DateTimeOffset? oldestInWeek = null;

            var logFiles = Directory
                .EnumerateFiles(logsDir, "unified*.jsonl", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .Where(file => file.Exists)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();

            if (logFiles.Count == 0)
            {
                return new AgentUsageInfo("Grok", AgentUsageStatus.NoData, null, "No Grok log files found.", null, Array.Empty<AgentUsageWindow>());
            }

            var scanned = 0;
            var maxLines = Math.Max(opts.MaxLinesToScan, 1);

            foreach (var file in logFiles)
            {
                if (scanned >= maxLines)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                await using var stream = OpenReadShared(file.FullName);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var content = await reader.ReadToEndAsync(cancellationToken);
                var lines = content.Split('\n');

                for (var i = lines.Length - 1; i >= 0; i--)
                {
                    if (scanned >= maxLines)
                    {
                        break;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    var line = lines[i].Trim();
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    scanned++;
                    if (!TryParseGrokLine(line, out var ts, out var freshTokens))
                    {
                        continue;
                    }

                    if (ts < weeklyStart)
                    {
                        break;
                    }

                    weeklyTokens += freshTokens;
                    if (oldestInWeek is null || ts < oldestInWeek)
                    {
                        oldestInWeek = ts;
                    }

                    if (ts >= fiveHourStart)
                    {
                        fiveHourTokens += freshTokens;
                        if (oldestInFiveHour is null || ts < oldestInFiveHour)
                        {
                            oldestInFiveHour = ts;
                        }
                    }
                }
            }

            var windows = new[]
            {
                new AgentUsageWindow(
                    "five_hour",
                    "Sessao 5h",
                    AgentUsageText.ClampPercent(100d * fiveHourTokens / opts.FiveHourTokens),
                    EstimateReset(oldestInFiveHour, TimeSpan.FromHours(5)),
                    300,
                    true,
                    fiveHourTokens,
                    opts.FiveHourTokens),
                new AgentUsageWindow(
                    "seven_day",
                    "Semana",
                    AgentUsageText.ClampPercent(100d * weeklyTokens / opts.WeeklyTokens),
                    EstimateReset(oldestInWeek, TimeSpan.FromDays(7)),
                    10080,
                    true,
                    weeklyTokens,
                    opts.WeeklyTokens)
            };

            return new AgentUsageInfo("Grok", AgentUsageStatus.Ok, null, "Estimated from local logs.", null, windows);
        }
        catch (Exception exception) when (exception is IOException
                                            or UnauthorizedAccessException
                                            or ArgumentException
                                            or NotSupportedException)
        {
            return new AgentUsageInfo(
                "Grok",
                AgentUsageStatus.Unavailable,
                null,
                AgentUsageText.Sanitize(exception.Message),
                null,
                Array.Empty<AgentUsageWindow>());
        }
    }

    private static bool TryParseGrokLine(string line, out DateTimeOffset ts, out long freshTokens)
    {
        ts = default;
        freshTokens = 0;
        try
        {
            var root = JObject.Parse(line);
            var tsRaw = root.Value<string>("ts");
            if (string.IsNullOrWhiteSpace(tsRaw) || !DateTimeOffset.TryParse(tsRaw, out ts))
            {
                return false;
            }

            ts = ts.ToUniversalTime();
            var ctx = root["ctx"] as JObject;
            if (ctx is null)
            {
                return false;
            }

            var promptTokens = ctx.Value<long?>("prompt_tokens") ?? 0;
            var cachedTokens = ctx.Value<long?>("cached_prompt_tokens") ?? 0;
            var completionTokens = ctx.Value<long?>("completion_tokens") ?? 0;
            freshTokens = Math.Max(promptTokens - cachedTokens, 0) + completionTokens;
            return true;
        }
        catch (Newtonsoft.Json.JsonException)
        {
            return false;
        }
    }

    private string? ResolveLogsDir()
    {
        if (!string.IsNullOrWhiteSpace(options.Value.Grok.LogPath))
        {
            return ExpandUserPath(options.Value.Grok.LogPath);
        }

        var grokHome = options.Value.Grok.GrokHome ?? Environment.GetEnvironmentVariable("GROK_HOME");
        if (!string.IsNullOrWhiteSpace(grokHome))
        {
            return Path.Combine(ExpandUserPath(grokHome), "logs");
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(profile) ? null : Path.Combine(profile, ".grok", "logs");
    }

    private static DateTimeOffset? EstimateReset(DateTimeOffset? oldest, TimeSpan window) =>
        oldest?.Add(window);

    private static string ExpandUserPath(string path)
    {
        if (path.StartsWith("~", StringComparison.Ordinal))
        {
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(profile, path[1..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        return Path.GetFullPath(path);
    }

    private static FileStream OpenReadShared(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
}
