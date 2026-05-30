using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Common.Interfaces;

public interface ILinkedDocumentNotifier
{
    Task LinkedDocumentLinkedAsync(LinkedDocumentDto document, Guid workingDirectoryId, CancellationToken cancellationToken);

    Task LinkedDocumentUpdatedAsync(LinkedDocumentDto document, Guid workingDirectoryId, CancellationToken cancellationToken);

    Task LinkedDocumentRemovedAsync(
        Guid linkedDocumentId,
        Guid promptId,
        Guid workingDirectoryId,
        CancellationToken cancellationToken);
}
