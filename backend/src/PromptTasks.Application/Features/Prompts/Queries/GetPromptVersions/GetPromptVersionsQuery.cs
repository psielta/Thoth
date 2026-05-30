using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Prompts.Queries.GetPromptVersions;

public sealed record GetPromptVersionsQuery(Guid PromptId) : IRequest<IReadOnlyList<PromptVersionDto>>;
