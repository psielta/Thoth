using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.LinkedDocuments.Commands.ResumeLinkedDocument;

public sealed class ResumeLinkedDocumentHandler(
    IApplicationDbContext context,
    ILinkedDocumentSyncService syncService,
    ILinkedDocumentWatchCoordinator watchCoordinator,
    ILinkedDocumentNotifier linkedDocumentNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ResumeLinkedDocumentCommand, LinkedDocumentDto>
{
    public async Task<LinkedDocumentDto> Handle(ResumeLinkedDocumentCommand request, CancellationToken cancellationToken)
    {
        var (document, prompt) = LinkedDocumentHelpers.GetDocument(context, request.Id, currentUser.UserId);
        LinkedDocumentHelpers.EnsurePromptAllowsTracking(prompt);

        document.Status = LinkedDocumentStatus.Tracking;
        document.UpdatedAtUtc = dateTimeProvider.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        var outcome = await syncService.SyncAsync(document.Id, LinkedDocumentVersionSource.Resumed, cancellationToken);
        var dto = outcome.Document ?? document.ToDto();
        if (dto.Status == LinkedDocumentStatus.Tracking)
        {
            await watchCoordinator.StartTrackingAsync(document.Id, cancellationToken);
        }
        else
        {
            watchCoordinator.StopTracking(document.Id);
        }

        await linkedDocumentNotifier.LinkedDocumentUpdatedAsync(dto, prompt.WorkingDirectoryId, cancellationToken);
        return dto;
    }
}
