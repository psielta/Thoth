using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.LinkedDocuments.Commands.LinkDocument;

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
        LinkedDocumentHelpers.EnsurePromptAllowsTracking(prompt);

        // Regra: cada prompt pode ter no maximo 1 plano vinculado (garantido tambem por indice unico no banco).
        if (context.LinkedDocuments.Any(document => document.PromptId == prompt.Id))
        {
            throw new ConflictException("Each prompt can have at most one linked plan. Remove the current plan before linking another.");
        }

        var validation = await fileService.ValidateAsync(request.AbsolutePath, cancellationToken);
        if (!validation.IsValid)
        {
            LinkedDocumentHelpers.ThrowValidation(nameof(request.AbsolutePath), validation.Error ?? "Invalid markdown file.");
        }

        var pathKey = validation.PathKey ?? string.Empty;
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
