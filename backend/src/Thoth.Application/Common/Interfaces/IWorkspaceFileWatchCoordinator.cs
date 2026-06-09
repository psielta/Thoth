namespace Thoth.Application.Common.Interfaces;

public interface IWorkspaceFileWatchCoordinator
{
    Task JoinFileAsync(Guid workingDirectoryId, string relativePath, string connectionId, CancellationToken cancellationToken);

    Task LeaveFileAsync(Guid workingDirectoryId, string relativePath, string connectionId, CancellationToken cancellationToken);

    void ReleaseConnection(string connectionId);
}