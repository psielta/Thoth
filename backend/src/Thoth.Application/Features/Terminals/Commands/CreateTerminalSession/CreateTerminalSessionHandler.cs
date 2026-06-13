using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Prompts;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.Terminals.Commands.CreateTerminalSession;

public sealed class CreateTerminalSessionHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    ITerminalSessionCoordinator terminalCoordinator)
    : IRequestHandler<CreateTerminalSessionCommand, TerminalSessionDescriptor>
{
    public async Task<TerminalSessionDescriptor> Handle(
        CreateTerminalSessionCommand request,
        CancellationToken cancellationToken)
    {
        var prompt = PromptMutationHelpers.GetPrompt(context, request.PromptId, currentUser.UserId);
        if (prompt.Status == PromptStatus.Archived)
        {
            throw new ForbiddenException("Cannot create terminal sessions for archived prompts.");
        }

        var directory = PromptMutationHelpers.GetWorkingDirectory(
            context,
            prompt.WorkingDirectoryId,
            currentUser.UserId);

        return await terminalCoordinator.CreateAsync(
            prompt.Id,
            directory.AbsolutePath,
            request.Shell ?? string.Empty,
            cancellationToken);
    }
}