using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Queries.GetAiModels;

public sealed class GetAiModelsHandler(IGeminiModelCatalog catalog)
    : IRequestHandler<GetAiModelsQuery, IReadOnlyList<GeminiModelDto>>
{
    public Task<IReadOnlyList<GeminiModelDto>> Handle(GetAiModelsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(catalog.GetModels());
}
