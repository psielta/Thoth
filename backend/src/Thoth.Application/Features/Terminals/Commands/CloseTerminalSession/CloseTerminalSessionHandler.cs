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

        _ = PromptMutationHelpers.GetPrompt(context, session.PromptId, currentUser.UserId);
        await terminalCoordinator.CloseAsync(request.SessionId, cancellationToken);
    }
}