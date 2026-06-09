using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Files.Queries.ReadFileContent;

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