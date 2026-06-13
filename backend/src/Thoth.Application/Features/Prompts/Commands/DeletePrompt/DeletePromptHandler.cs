using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Features.Ai.Commands.ReleasePromptAiSessions;
using Thoth.Application.Features.Prompts;

namespace Thoth.Application.Features.Prompts.Commands.DeletePrompt;

public sealed class DeletePromptHandler(
    IApplicationDbContext context,
    IPromptNotifier promptNotifier,
    ITerminalSessionCoordinator terminalCoordinator,
    ICurrentUser currentUser,
    ISender sender)
    : IRequestHandler<DeletePromptCommand>
{
    public async Task Handle(DeletePromptCommand request, CancellationToken cancellationToken)
    {
        var prompt = PromptMutationHelpers.GetPrompt(context, request.Id, currentUser.UserId);
        var workingDirectoryId = prompt.WorkingDirectoryId;

        await sender.Send(new ReleasePromptAiSessionsCommand(prompt.Id), cancellationToken);
        await terminalCoordinator.KillForPromptAsync(prompt.Id, cancellationToken);

        context.Remove(prompt);
        await context.SaveChangesAsync(cancellationToken);
        await promptNotifier.PromptDeletedAsync(prompt.Id, workingDirectoryId, cancellationToken);
    }
}
