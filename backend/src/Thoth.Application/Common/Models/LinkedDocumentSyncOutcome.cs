namespace Thoth.Application.Common.Models;

public sealed record LinkedDocumentSyncOutcome(
    LinkedDocumentDto? Document,
    Guid? PromptWorkingDirectoryId,
    bool Changed,
    bool StatusChanged,
    bool MissingDocument = false);
