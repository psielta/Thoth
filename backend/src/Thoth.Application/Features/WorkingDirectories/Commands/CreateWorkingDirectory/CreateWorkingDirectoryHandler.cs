using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Domain.WorkingDirectories;

namespace Thoth.Application.Features.WorkingDirectories.Commands.CreateWorkingDirectory;

public sealed class CreateWorkingDirectoryHandler(
    IApplicationDbContext context,
    IWorkspaceFileService workspaceFileService,
    ICurrentUser currentUser)
    : IRequestHandler<CreateWorkingDirectoryCommand, WorkingDirectoryDto>
{
    public async Task<WorkingDirectoryDto> Handle(CreateWorkingDirectoryCommand request, CancellationToken cancellationToken)
    {
        var path = await workspaceFileService.ValidatePathAsync(request.AbsolutePath, cancellationToken);
        if (!path.IsValid || path.CanonicalPath is null)
        {
            throw new PathTraversalException(path.Error ?? "Invalid working directory path.");
        }

        if (context.WorkingDirectories
            .Where(directory => directory.OwnerId == currentUser.UserId)
            .AsEnumerable()
            .Any(directory => directory.AbsolutePath.Equals(path.CanonicalPath, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("This working directory is already registered.");
        }

        var workingDirectory = new WorkingDirectory
        {
            Name = request.Name.Trim(),
            AbsolutePath = path.CanonicalPath,
            RespectGitignore = request.RespectGitignore,
            EnableAiContext = request.EnableAiContext,
            TaskNumberPattern = NormalizePattern(request.TaskNumberPattern),
            OwnerId = currentUser.UserId
        };

        context.Add(workingDirectory);
        await context.SaveChangesAsync(cancellationToken);

        return workingDirectory.ToDto();
    }

    private static string? NormalizePattern(string? pattern) =>
        string.IsNullOrWhiteSpace(pattern) ? null : pattern.Trim();
}
