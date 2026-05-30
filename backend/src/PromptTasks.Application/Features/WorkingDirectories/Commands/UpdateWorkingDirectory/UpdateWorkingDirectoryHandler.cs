using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.WorkingDirectories.Commands.UpdateWorkingDirectory;

public sealed class UpdateWorkingDirectoryHandler(
    IApplicationDbContext context,
    IWorkspaceFileService workspaceFileService,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateWorkingDirectoryCommand, WorkingDirectoryDto>
{
    public async Task<WorkingDirectoryDto> Handle(UpdateWorkingDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        var path = await workspaceFileService.ValidatePathAsync(request.AbsolutePath, cancellationToken);
        if (!path.IsValid || path.CanonicalPath is null)
        {
            throw new PathTraversalException(path.Error ?? "Invalid working directory path.");
        }

        if (context.WorkingDirectories
            .Where(item => item.Id != request.Id && item.OwnerId == currentUser.UserId)
            .AsEnumerable()
            .Any(item => item.AbsolutePath.Equals(path.CanonicalPath, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("This working directory is already registered.");
        }

        directory.Name = request.Name.Trim();
        directory.AbsolutePath = path.CanonicalPath;
        directory.RespectGitignore = request.RespectGitignore;

        await context.SaveChangesAsync(cancellationToken);
        return directory.ToDto();
    }
}
