using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Terminals.Queries.ListGenericTerminalSessions;

public sealed class ListGenericTerminalSessionsHandler(
    ICurrentUser currentUser,
    ITerminalSessionCoordinator terminalCoordinator)
    : IRequestHandler<ListGenericTerminalSessionsQuery, IReadOnlyList<TerminalSessionDescriptor>>
{
    public Task<IReadOnlyList<TerminalSessionDescriptor>> Handle(
        ListGenericTerminalSessionsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(terminalCoordinator.ListForOwner(currentUser.UserId));
}
