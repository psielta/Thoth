using PromptTasks.Application.Features.Ai.Models;

namespace PromptTasks.Application.Common.Interfaces;

public interface IGeminiModelCatalog
{
    IReadOnlyList<GeminiModelDto> GetModels();
    GeminiModelDto? GetModel(string id);
}
