using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Common.Interfaces;

public interface ILinkedDocumentSyncService
{
    Task<LinkedDocumentSyncOutcome> SyncAsync(
        Guid linkedDocumentId,
        LinkedDocumentVersionSource source,
        CancellationToken cancellationToken);
}
