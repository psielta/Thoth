namespace Thoth.Infrastructure.Ai;

public sealed class GeminiOptions
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
    public string DefaultModel { get; set; } = "gemini-2.5-flash";
    public int RequestTimeoutSeconds { get; set; } = 300;
    public int StreamTimeoutSeconds { get; set; } = 300;
    public int SystemCacheTtlSeconds { get; set; } = 3600;
    public int SessionCacheTtlSeconds { get; set; } = 1800;
    public int SessionCacheMinTokens { get; set; } = 4096;
    public string SystemInstruction { get; set; } = "";
    public List<GeminiModelOption> Models { get; set; } = new();
}

public sealed class GeminiModelOption
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string ThinkingMode { get; set; } = "none";
    public bool CanDisableThinking { get; set; }
    public int ThinkingBudgetMin { get; set; }
    public int ThinkingBudgetMax { get; set; }
    public int MinCacheTokens { get; set; } = 1024;
}
