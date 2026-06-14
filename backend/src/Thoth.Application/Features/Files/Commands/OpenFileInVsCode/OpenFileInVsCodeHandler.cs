using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;

namespace Thoth.Application.Features.Files.Commands.OpenFileInVsCode;

public sealed class OpenFileInVsCodeHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IWorkspaceFileService workspaceFileService,
    IWorkspaceEditorLauncher editorLauncher)
    : IRequestHandler<OpenFileInVsCodeCommand>
{
    public async Task Handle(OpenFileInVsCodeCommand request, CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.WorkingDirectoryId && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        var fileAbsolutePath = await workspaceFileService.ResolveExistingFilePathAsync(
            directory.AbsolutePath,
            request.RelativePath,
            cancellationToken);

        try
        {
            await editorLauncher.OpenFileInVsCodeAsync(directory.AbsolutePath, fileAbsolutePath, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictException(exception.Message);
        }
    }
}
