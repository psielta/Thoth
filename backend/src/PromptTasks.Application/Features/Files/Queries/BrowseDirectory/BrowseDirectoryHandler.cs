using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Files.Queries.BrowseDirectory;

public sealed class BrowseDirectoryHandler(
    IApplicationDbContext context,
    IWorkspaceFileService workspaceFileService,
    ICurrentUser currentUser)
    : IRequestHandler<BrowseDirectoryQuery, IReadOnlyList<DirectoryEntryDto>>
{
    public async Task<IReadOnlyList<DirectoryEntryDto>> Handle(BrowseDirectoryQuery request, CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.WorkingDirectoryId && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        try
        {
            return await workspaceFileService.BrowseDirectoryAsync(
                directory.AbsolutePath,
                request.RelativePath,
                directory.RespectGitignore,
                cancellationToken);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("Directory was not found.");
        }
    }
}