using Microsoft.Extensions.Options;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Infrastructure.Ai;

public sealed class GeminiModelCatalog(IOptions<GeminiOptions> options) : IGeminiModelCatalog
{
    public IReadOnlyList<GeminiModelDto> GetModels() =>
        options.Value.Models
            .Select(m => new GeminiModelDto(
                m.Id,
                m.Label,
                m.ThinkingMode,
                m.CanDisableThinking,
                m.ThinkingBudgetMin,
                m.ThinkingBudgetMax,
                m.MinCacheTokens))
            .ToList();

    public GeminiModelDto? GetModel(string id) =>
        GetModels().FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
}
