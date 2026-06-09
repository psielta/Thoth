using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.LinkedDocuments.Commands.RefreshLinkedDocument;

public sealed class RefreshLinkedDocumentHandler(
    IApplicationDbContext context,
    ILinkedDocumentSyncService syncService,
    ILinkedDocumentWatchCoordinator watchCoordinator,
    ILinkedDocumentNotifier linkedDocumentNotifier,
    ICurrentUser currentUser)
    : IRequestHandler<RefreshLinkedDocumentCommand, LinkedDocumentDto>
{
    public async Task<LinkedDocumentDto> Handle(RefreshLinkedDocumentCommand request, CancellationToken cancellationToken)
    {
        var (_, prompt) = LinkedDocumentHelpers.GetDocument(context, request.Id, currentUser.UserId);
        var outcome = await syncService.SyncAsync(request.Id, LinkedDocumentVersionSource.ManualRefresh, cancellationToken);
        if (outcome.Document is null)
        {
            throw new NotFoundException("Linked document was not found.");
        }

        if (outcome.Document.Status == LinkedDocumentStatus.Tracking)
        {
            await watchCoordinator.StartTrackingAsync(request.Id, cancellationToken);
        }
        else
        {
            watchCoordinator.StopTracking(request.Id);
        }

        await linkedDocumentNotifier.LinkedDocumentUpdatedAsync(outcome.Document, prompt.WorkingDirectoryId, cancellationToken);
        return outcome.Document;
    }
}
