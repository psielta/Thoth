using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Files.Queries.SearchFiles;

public sealed class SearchFilesHandler(
    IApplicationDbContext context,
    IWorkspaceFileService workspaceFileService,
    ICurrentUser currentUser)
    : IRequestHandler<SearchFilesQuery, IReadOnlyList<FileSearchResultDto>>
{
    public async Task<IReadOnlyList<FileSearchResultDto>> Handle(SearchFilesQuery request, CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.WorkingDirectoryId && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        return await workspaceFileService.SearchAsync(
            directory.Id,
            directory.AbsolutePath,
            request.Query ?? string.Empty,
            request.Limit,
            directory.RespectGitignore,
            cancellationToken);
    }
}
