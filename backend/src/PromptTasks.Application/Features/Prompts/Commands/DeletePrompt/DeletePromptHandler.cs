using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Features.Prompts;

namespace PromptTasks.Application.Features.Prompts.Commands.DeletePrompt;

public sealed class DeletePromptHandler(
    IApplicationDbContext context,
    IPromptNotifier promptNotifier,
    ICurrentUser currentUser)
    : IRequestHandler<DeletePromptCommand>
{
    public async Task Handle(DeletePromptCommand request, CancellationToken cancellationToken)
    {
        var prompt = PromptMutationHelpers.GetPrompt(context, request.Id, currentUser.UserId);
        var workingDirectoryId = prompt.WorkingDirectoryId;

        context.Remove(prompt);
        await context.SaveChangesAsync(cancellationToken);
        await promptNotifier.PromptDeletedAsync(prompt.Id, workingDirectoryId, cancellationToken);
    }
}
