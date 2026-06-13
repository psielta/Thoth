using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Terminals;

namespace Thoth.Application.Features.Terminals.Commands.CreateGenericTerminalSession;

public sealed class CreateGenericTerminalSessionHandler(
    ICurrentUser currentUser,
    ITerminalSessionCoordinator terminalCoordinator)
    : IRequestHandler<CreateGenericTerminalSessionCommand, TerminalSessionDescriptor>
{
    public Task<TerminalSessionDescriptor> Handle(
        CreateGenericTerminalSessionCommand request,
        CancellationToken cancellationToken)
    {
        // Claude Plan stages the parent prompt's content into the launch; a generic
        // terminal has no prompt, so the option is not available here.
        if (request.AgentLaunch == TerminalAgentLaunch.ClaudePlan)
        {
            throw new ForbiddenException(
                "Claude Plan requires a prompt and is not available for generic terminals.");
        }

        var initialInput = TerminalAgentLaunchCommands.ResolveInitialInput(request.AgentLaunch);
        var followUpInput = TerminalAgentLaunchCommands.ResolveFollowUpInput(request.AgentLaunch);

        return terminalCoordinator.CreateGenericAsync(
            currentUser.UserId,
            cwd: null,
            request.Shell ?? string.Empty,
            initialInput,
            cancellationToken,
            followUpInput);
    }
}
