using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Prompts.Queries.GetPromptVersions;

public sealed record GetPromptVersionsQuery(Guid PromptId) : IRequest<IReadOnlyList<PromptVersionDto>>;
