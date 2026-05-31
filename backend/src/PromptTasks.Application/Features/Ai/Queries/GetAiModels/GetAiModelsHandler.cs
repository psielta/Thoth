using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Features.Ai.Models;

namespace PromptTasks.Application.Features.Ai.Queries.GetAiModels;

public sealed class GetAiModelsHandler(IGeminiModelCatalog catalog)
    : IRequestHandler<GetAiModelsQuery, IReadOnlyList<GeminiModelDto>>
{
    public Task<IReadOnlyList<GeminiModelDto>> Handle(GetAiModelsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(catalog.GetModels());
}
