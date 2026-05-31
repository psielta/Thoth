using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Common.Interfaces;

public interface IGeminiClient
{
    Task<GeminiResult> RefineAsync(GeminiGenerationRequest request, CancellationToken ct);
    IAsyncEnumerable<GeminiStreamChunk> StreamAsync(GeminiGenerationRequest request, CancellationToken ct);
    Task<GeminiCacheHandle?> EnsureSessionCacheAsync(string model, string systemInstruction, IReadOnlyList<GeminiTurn> history, CancellationToken ct);
    Task DeleteCacheAsync(string name, CancellationToken ct);
}
