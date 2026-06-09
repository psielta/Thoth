using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Infrastructure.FileSystem;

public sealed class LinkedDocumentSyncService(
    IApplicationDbContext context,
    ILinkedDocumentFileService fileService,
    IDateTimeProvider dateTimeProvider)
    : ILinkedDocumentSyncService
{
    public async Task<LinkedDocumentSyncOutcome> SyncAsync(
        Guid linkedDocumentId,
        LinkedDocumentVersionSource source,
        CancellationToken cancellationToken)
    {
        var document = context.LinkedDocuments.FirstOrDefault(item => item.Id == linkedDocumentId);
        if (document is null)
        {
            return new LinkedDocumentSyncOutcome(null, null, false, false, MissingDocument: true);
        }

        var prompt = context.Prompts.FirstOrDefault(item => item.Id == document.PromptId);
        if (prompt is null)
        {
            return new LinkedDocumentSyncOutcome(null, null, false, false, MissingDocument: true);
        }

        if (document.Status == LinkedDocumentStatus.Paused)
        {
            return new LinkedDocumentSyncOutcome(document.ToDto(), prompt.WorkingDirectoryId, false, false);
        }

        var previousStatus = document.Status;
        var previousError = document.LastError;
        var previousHash = document.LastContentHash;
        var readResult = await fileService.ReadAsync(document.AbsolutePath, cancellationToken);
        var now = dateTimeProvider.UtcNow;

        if (!readResult.Success)
        {
            document.Status = readResult.FileMissing ? LinkedDocumentStatus.Missing : LinkedDocumentStatus.Error;
            document.LastError = readResult.Error ?? "Markdown file could not be read.";
            document.UpdatedAtUtc = now;
            await context.SaveChangesAsync(cancellationToken);

            return new LinkedDocumentSyncOutcome(
                document.ToDto(),
                prompt.WorkingDirectoryId,
                false,
                previousStatus != document.Status || previousError != document.LastError);
        }

        document.LastSyncedAtUtc = now;
        document.SizeBytes = readResult.SizeBytes;
        document.Status = LinkedDocumentStatus.Tracking;
        document.LastError = null;
        document.UpdatedAtUtc = now;

        var contentChanged = !string.Equals(previousHash, readResult.ContentHash, StringComparison.OrdinalIgnoreCase);
        if (contentChanged)
        {
            document.CurrentVersion++;
            document.LastContentHash = readResult.ContentHash;
            context.Add(new LinkedDocumentVersion
            {
                LinkedDocumentId = document.Id,
                VersionNumber = document.CurrentVersion,
                Content = readResult.Content ?? string.Empty,
                ContentHash = readResult.ContentHash ?? string.Empty,
                SizeBytes = readResult.SizeBytes ?? 0,
                Source = source,
                CreatedAtUtc = now
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return new LinkedDocumentSyncOutcome(
            document.ToDto(),
            prompt.WorkingDirectoryId,
            contentChanged,
            previousStatus != document.Status || previousError != document.LastError);
    }
}
