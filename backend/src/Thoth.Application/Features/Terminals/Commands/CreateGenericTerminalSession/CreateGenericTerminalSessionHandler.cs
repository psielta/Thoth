using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Terminals;

namespace Thoth.Application.Features.Terminals.Commands.CreateGenericTerminalSession;

public sealed class CreateGenericTerminalSessionHandler(
    IApplicationDbContext context,
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
        string? cwd = null;
        if (request.WorkingDirectoryId is { } workingDirectoryId)
        {
            var directory = context.WorkingDirectories
                .FirstOrDefault(item => item.Id == workingDirectoryId && item.OwnerId == currentUser.UserId);

            if (directory is null)
            {
                throw new NotFoundException("Working directory was not found.");
            }

            cwd = directory.AbsolutePath;
        }

        return terminalCoordinator.CreateGenericAsync(
            currentUser.UserId,
            cwd,
            request.Shell ?? string.Empty,
            initialInput,
            cancellationToken,
            followUpInput);
    }
}
