using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;

namespace PromptTasks.Application.Features.WorkingDirectories.Commands.DeleteWorkingDirectory;

public sealed class DeleteWorkingDirectoryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<DeleteWorkingDirectoryCommand>
{
    public async Task Handle(DeleteWorkingDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        context.Remove(directory);
        await context.SaveChangesAsync(cancellationToken);
    }
}
