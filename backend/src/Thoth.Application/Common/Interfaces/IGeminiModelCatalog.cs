using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Common.Interfaces;

public interface IGeminiModelCatalog
{
    IReadOnlyList<GeminiModelDto> GetModels();
    GeminiModelDto? GetModel(string id);
}
