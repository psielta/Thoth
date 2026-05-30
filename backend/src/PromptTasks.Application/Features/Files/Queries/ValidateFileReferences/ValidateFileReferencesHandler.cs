using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Files.Queries.ValidateFileReferences;

public sealed class ValidateFileReferencesHandler(
    IApplicationDbContext context,
    IWorkspaceFileService workspaceFileService,
    ICurrentUser currentUser)
    : IRequestHandler<ValidateFileReferencesQuery, IReadOnlyList<FileReferenceValidationDto>>
{
    public async Task<IReadOnlyList<FileReferenceValidationDto>> Handle(
        ValidateFileReferencesQuery request,
        CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.WorkingDirectoryId && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        return await workspaceFileService.ValidateRelativePathsAsync(
            directory.AbsolutePath,
            request.RelativePaths,
            cancellationToken);
    }
}
