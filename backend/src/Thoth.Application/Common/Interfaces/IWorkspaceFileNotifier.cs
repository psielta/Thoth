namespace Thoth.Application.Common.Interfaces;

public interface IWorkspaceFileNotifier
{
    Task WorkspaceFileChangedAsync(Guid workingDirectoryId, string relativePath, CancellationToken cancellationToken);
}