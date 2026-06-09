using MediatR;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Queries.ListChatSessions;

public sealed record ListChatSessionsQuery(Guid? WorkingDirectoryId, Guid? PromptId) : IRequest<IReadOnlyList<AiChatSessionDto>>;
