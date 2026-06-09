using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.LinkedDocuments.Commands.PauseLinkedDocument;

public sealed class PauseLinkedDocumentHandler(
    IApplicationDbContext context,
    ILinkedDocumentWatchCoordinator watchCoordinator,
    ILinkedDocumentNotifier linkedDocumentNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<PauseLinkedDocumentCommand, LinkedDocumentDto>
{
    public async Task<LinkedDocumentDto> Handle(PauseLinkedDocumentCommand request, CancellationToken cancellationToken)
    {
        var (document, prompt) = LinkedDocumentHelpers.GetDocument(context, request.Id, currentUser.UserId);
        document.Status = LinkedDocumentStatus.Paused;
        document.UpdatedAtUtc = dateTimeProvider.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        watchCoordinator.StopTracking(document.Id);

        var dto = document.ToDto();
        await linkedDocumentNotifier.LinkedDocumentUpdatedAsync(dto, prompt.WorkingDirectoryId, cancellationToken);
        return dto;
    }
}
