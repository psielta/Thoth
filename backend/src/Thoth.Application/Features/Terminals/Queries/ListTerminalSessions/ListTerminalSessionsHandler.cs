using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Prompts;

namespace Thoth.Application.Features.Terminals.Queries.ListTerminalSessions;

public sealed class ListTerminalSessionsHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    ITerminalSessionCoordinator terminalCoordinator)
    : IRequestHandler<ListTerminalSessionsQuery, IReadOnlyList<TerminalSessionDescriptor>>
{
    public Task<IReadOnlyList<TerminalSessionDescriptor>> Handle(
        ListTerminalSessionsQuery request,
        CancellationToken cancellationToken)
    {
        _ = PromptMutationHelpers.GetPrompt(context, request.PromptId, currentUser.UserId);
        return Task.FromResult(terminalCoordinator.ListForPrompt(request.PromptId));
    }
}