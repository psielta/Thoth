using MediatR;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Queries.GetAiModels;

public sealed record GetAiModelsQuery : IRequest<IReadOnlyList<GeminiModelDto>>;
