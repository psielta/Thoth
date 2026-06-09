using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;

namespace Thoth.Application.Features.Ai.Commands.DeleteChatSession;

public sealed class DeleteChatSessionHandler(
    IApplicationDbContext context,
    IGeminiClient gemini,
    ICurrentUser currentUser)
    : IRequestHandler<DeleteChatSessionCommand>
{
    public async Task Handle(DeleteChatSessionCommand request, CancellationToken cancellationToken)
    {
        var session = context.AiChatSessions
            .FirstOrDefault(s => s.Id == request.SessionId && s.OwnerId == currentUser.UserId)
            ?? throw new NotFoundException($"Sessão {request.SessionId} não encontrada.");

        var cacheName = session.GeminiCacheName;

        var messages = context.AiChatMessages.Where(m => m.SessionId == session.Id).ToList();
        foreach (var message in messages)
            context.Remove(message);

        context.Remove(session);
        await context.SaveChangesAsync(cancellationToken);

        if (cacheName is not null)
        {
            try { await gemini.DeleteCacheAsync(cacheName, cancellationToken); }
            catch { /* best effort */ }
        }
    }
}
