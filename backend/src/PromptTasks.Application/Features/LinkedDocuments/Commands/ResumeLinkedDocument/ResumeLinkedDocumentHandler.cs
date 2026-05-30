using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.ResumeLinkedDocument;

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
