using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Common.Interfaces;

public interface ILinkedDocumentSyncService
{
    Task<LinkedDocumentSyncOutcome> SyncAsync(
        Guid linkedDocumentId,
        LinkedDocumentVersionSource source,
        CancellationToken cancellationToken);
}
