using MediatR;
using PromptTasks.Application.Features.Ai.Models;

namespace PromptTasks.Application.Features.Ai.Queries.GetAiModels;

public sealed record GetAiModelsQuery : IRequest<IReadOnlyList<GeminiModelDto>>;
