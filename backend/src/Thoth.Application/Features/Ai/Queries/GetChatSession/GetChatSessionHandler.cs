using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Queries.GetChatSession;

public sealed class GetChatSessionHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetChatSessionQuery, AiChatSessionDto>
{
    public Task<AiChatSessionDto> Handle(GetChatSessionQuery request, CancellationToken cancellationToken)
    {
        var session = context.AiChatSessions
            .FirstOrDefault(s => s.Id == request.Id && s.OwnerId == currentUser.UserId)
            ?? throw new NotFoundException($"Sessão {request.Id} não encontrada.");

        var messages = context.AiChatMessages
            .Where(m => m.SessionId == session.Id)
            .OrderBy(m => m.Sequence)
            .Select(m => new AiChatMessageDto(m.Id, m.Role, m.Content, m.Sequence, m.CachedTokens, m.CreatedAtUtc))
            .ToList();

        return Task.FromResult(new AiChatSessionDto(
            session.Id,
            session.WorkingDirectoryId,
            session.PromptId,
            session.Title,
            session.Model,
            session.Temperature,
            session.ThinkingEnabled,
            session.ThinkingBudget,
            session.ThinkingLevel,
            session.CreatedAtUtc,
            messages));
    }
}
