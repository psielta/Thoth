using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Features.Prompts;

namespace Thoth.Application.Features.Terminals.Commands.CloseTerminalSession;

public sealed class CloseTerminalSessionHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    ITerminalSessionCoordinator terminalCoordinator)
    : IRequestHandler<CloseTerminalSessionCommand>
{
    public async Task Handle(CloseTerminalSessionCommand request, CancellationToken cancellationToken)
    {
        var session = terminalCoordinator.TryGetSession(request.SessionId)
            ?? throw new NotFoundException("Terminal session was not found.");

        if (session.PromptId is { } promptId)
        {
            // Prompt-scoped session: ownership flows through the prompt.
            _ = PromptMutationHelpers.GetPrompt(context, promptId, currentUser.UserId);
        }
        else if (terminalCoordinator.ListForOwner(currentUser.UserId).All(item => item.Id != request.SessionId))
        {
            // Generic session: it must belong to the current user.
            throw new NotFoundException("Terminal session was not found.");
        }

        await terminalCoordinator.CloseAsync(request.SessionId, cancellationToken);
    }
}