using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Queries.ListChatSessions;

public sealed class ListChatSessionsHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<ListChatSessionsQuery, IReadOnlyList<AiChatSessionDto>>
{
    public Task<IReadOnlyList<AiChatSessionDto>> Handle(ListChatSessionsQuery request, CancellationToken cancellationToken)
    {
        var query = context.AiChatSessions.Where(s => s.OwnerId == currentUser.UserId);

        if (request.WorkingDirectoryId.HasValue)
            query = query.Where(s => s.WorkingDirectoryId == request.WorkingDirectoryId);

        if (request.PromptId.HasValue)
            query = query.Where(s => s.PromptId == request.PromptId);

        var result = query
            .OrderByDescending(s => s.UpdatedAtUtc)
            .Select(s => new AiChatSessionDto(
                s.Id,
                s.WorkingDirectoryId,
                s.PromptId,
                s.Title,
                s.Model,
                s.Temperature,
                s.ThinkingEnabled,
                s.ThinkingBudget,
                s.ThinkingLevel,
                s.CreatedAtUtc,
                new List<AiChatMessageDto>()))
            .ToList();

        return Task.FromResult<IReadOnlyList<AiChatSessionDto>>(result);
    }
}
