using MediatR;
using PromptTasks.Application.Common.Interfaces;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.RemoveLinkedDocument;

public sealed class RemoveLinkedDocumentHandler(
    IApplicationDbContext context,
    ILinkedDocumentWatchCoordinator watchCoordinator,
    ILinkedDocumentNotifier linkedDocumentNotifier,
    ICurrentUser currentUser)
    : IRequestHandler<RemoveLinkedDocumentCommand>
{
    public async Task Handle(RemoveLinkedDocumentCommand request, CancellationToken cancellationToken)
    {
        var (document, prompt) = LinkedDocumentHelpers.GetDocument(context, request.Id, currentUser.UserId);
        var documentId = document.Id;
        var promptId = prompt.Id;
        var workingDirectoryId = prompt.WorkingDirectoryId;

        watchCoordinator.StopTracking(documentId);
        context.Remove(document);
        await context.SaveChangesAsync(cancellationToken);

        await linkedDocumentNotifier.LinkedDocumentRemovedAsync(
            documentId,
            promptId,
            workingDirectoryId,
            cancellationToken);
    }
}
