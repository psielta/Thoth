using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Files.Queries.ReadFileContent;

public sealed class ReadFileContentHandler(
    IApplicationDbContext context,
    IWorkspaceFileService workspaceFileService,
    ICurrentUser currentUser)
    : IRequestHandler<ReadFileContentQuery, FileContentDto>
{
    public async Task<FileContentDto> Handle(ReadFileContentQuery request, CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.WorkingDirectoryId && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        try
        {
            return await workspaceFileService.ReadFileAsync(
                directory.AbsolutePath,
                request.RelativePath,
                cancellationToken);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("File was not found.");
        }
    }
}