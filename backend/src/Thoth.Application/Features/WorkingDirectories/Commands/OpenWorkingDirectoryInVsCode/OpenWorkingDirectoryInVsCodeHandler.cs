using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;

namespace Thoth.Application.Features.WorkingDirectories.Commands.OpenWorkingDirectoryInVsCode;

public sealed class OpenWorkingDirectoryInVsCodeHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IWorkspaceEditorLauncher editorLauncher)
    : IRequestHandler<OpenWorkingDirectoryInVsCodeCommand>
{
    public async Task Handle(OpenWorkingDirectoryInVsCodeCommand request, CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        try
        {
            await editorLauncher.OpenVsCodeAsync(directory.AbsolutePath, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictException(exception.Message);
        }
    }
}
