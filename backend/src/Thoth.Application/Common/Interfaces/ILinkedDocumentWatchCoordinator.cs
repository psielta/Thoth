namespace Thoth.Application.Common.Interfaces;

public interface ILinkedDocumentWatchCoordinator
{
    Task StartTrackingAsync(Guid linkedDocumentId, CancellationToken cancellationToken);

    void StopTracking(Guid linkedDocumentId);
}
