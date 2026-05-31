namespace PromptTasks.Application.Common.Models;

public sealed record GeminiThinking(string Mode, int? Budget, string? Level);
public sealed record GeminiTurn(string Role, string Text);
public sealed record GeminiGenerationRequest(
    string Model,
    double Temperature,
    GeminiThinking Thinking,
    bool IncludeThoughts,
    bool UseSystemCache,
    string? CachedContentName,
    string? SystemInstruction,
    IReadOnlyList<GeminiTurn> Contents);
public sealed record GeminiResult(string Text, int PromptTokens, int CandidateTokens, int CachedTokens);
public sealed record GeminiStreamChunk(string Text, bool IsThought, bool Done, int? CachedTokens);
public sealed record GeminiCacheHandle(string Name, DateTimeOffset ExpiresAt);
