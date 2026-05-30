using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.LinkDocument;

public sealed class LinkDocumentHandler(
    IApplicationDbContext context,
    ILinkedDocumentFileService fileService,
    ILinkedDocumentWatchCoordinator watchCoordinator,
    ILinkedDocumentNotifier linkedDocumentNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<LinkDocumentCommand, LinkedDocumentDto>
{
    public async Task<LinkedDocumentDto> Handle(LinkDocumentCommand request, CancellationToken cancellationToken)
    {
        var prompt = LinkedDocumentHelpers.GetPrompt(context, request.PromptId, currentUser.UserId);
        var validation = await fileService.ValidateAsync(request.AbsolutePath, cancellationToken);
        if (!validation.IsValid)
        {
            LinkedDocumentHelpers.ThrowValidation(nameof(request.AbsolutePath), validation.Error ?? "Invalid markdown file.");
        }

        var pathKey = validation.PathKey ?? string.Empty;
        if (context.LinkedDocuments.Any(document => document.PromptId == prompt.Id && document.AbsolutePathKey == pathKey))
        {
            throw new ConflictException("This markdown file is already linked to the prompt.");
        }

        var readResult = await fileService.ReadAsync(validation.CanonicalPath ?? request.AbsolutePath, cancellationToken);
        if (!readResult.Success)
        {
            LinkedDocumentHelpers.ThrowValidation(nameof(request.AbsolutePath), readResult.Error ?? "Markdown file could not be read.");
        }

        var now = dateTimeProvider.UtcNow;
        var canonicalPath = validation.CanonicalPath ?? request.AbsolutePath;
        var document = new LinkedDocument
        {
            PromptId = prompt.Id,
            WorkingDirectoryId = prompt.WorkingDirectoryId,
            AbsolutePath = canonicalPath,
            AbsolutePathKey = pathKey,
            DocumentType = request.DocumentType,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? Path.GetFileName(canonicalPath) : request.DisplayName.Trim(),
            Status = LinkedDocumentStatus.Tracking,
            CurrentVersion = 1,
            LastContentHash = readResult.ContentHash,
            LastSyncedAtUtc = now,
            SizeBytes = readResult.SizeBytes,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.Add(document);
        context.Add(LinkedDocumentHelpers.CreateVersion(document, readResult, LinkedDocumentVersionSource.Initial, now));
        await context.SaveChangesAsync(cancellationToken);

        await watchCoordinator.StartTrackingAsync(document.Id, cancellationToken);

        var dto = document.ToDto();
        await linkedDocumentNotifier.LinkedDocumentLinkedAsync(dto, prompt.WorkingDirectoryId, cancellationToken);
        return dto;
    }
}
