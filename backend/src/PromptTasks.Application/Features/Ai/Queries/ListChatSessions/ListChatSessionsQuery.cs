using MediatR;
using PromptTasks.Application.Features.Ai.Models;

namespace PromptTasks.Application.Features.Ai.Queries.ListChatSessions;

public sealed record ListChatSessionsQuery(Guid? WorkingDirectoryId, Guid? PromptId) : IRequest<IReadOnlyList<AiChatSessionDto>>;
