using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Ai.Commands.ReleasePromptAiSessions;
using Thoth.Application.Features.Prompts;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.Prompts.Commands.UpdatePromptStatus;

public sealed class UpdatePromptStatusHandler(
    IApplicationDbContext context,
    IPromptNotifier promptNotifier,
    ILinkedDocumentWatchCoordinator watchCoordinator,
    ILinkedDocumentNotifier linkedDocumentNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    ISender sender)
    : IRequestHandler<UpdatePromptStatusCommand, PromptDto>
{
    public async Task<PromptDto> Handle(UpdatePromptStatusCommand request, CancellationToken cancellationToken)
    {
        var prompt = PromptMutationHelpers.GetPrompt(context, request.Id, currentUser.UserId);
        PromptMutationHelpers.EnsureRowVersion(prompt, request.RowVersion);

        prompt.Status = request.Status;
        prompt.CurrentVersion++;
        var pausedLinkedDocuments = PromptMutationHelpers.PauseLinkedDocumentsIfPromptIsArchived(
            context,
            prompt,
            dateTimeProvider);

        context.Add(PromptMutationHelpers.CreateVersion(prompt, dateTimeProvider, "Status changed"));
        await context.SaveChangesAsync(cancellationToken);

        if (prompt.Status == PromptStatus.Archived)
            await sender.Send(new ReleasePromptAiSessionsCommand(prompt.Id), cancellationToken);

        var references = context.PromptFileReferences
            .Where(reference => reference.PromptId == prompt.Id)
            .ToList();
        var dto = prompt.ToDto(references);
        await promptNotifier.PromptUpdatedAsync(dto, cancellationToken);

        foreach (var document in pausedLinkedDocuments)
        {
            watchCoordinator.StopTracking(document.Id);
            await linkedDocumentNotifier.LinkedDocumentUpdatedAsync(document.ToDto(), prompt.WorkingDirectoryId, cancellationToken);
        }

        return dto;
    }
}
