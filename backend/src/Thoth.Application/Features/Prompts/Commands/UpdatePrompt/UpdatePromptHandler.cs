using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Prompts;

namespace Thoth.Application.Features.Prompts.Commands.UpdatePrompt;

public sealed class UpdatePromptHandler(
    IApplicationDbContext context,
    IWorkspaceFileService workspaceFileService,
    IPromptNotifier promptNotifier,
    ILinkedDocumentWatchCoordinator watchCoordinator,
    ILinkedDocumentNotifier linkedDocumentNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdatePromptCommand, PromptDto>
{
    public async Task<PromptDto> Handle(UpdatePromptCommand request, CancellationToken cancellationToken)
    {
        var prompt = PromptMutationHelpers.GetPrompt(context, request.Id, currentUser.UserId);
        PromptMutationHelpers.EnsureRowVersion(prompt, request.RowVersion);
        var directory = PromptMutationHelpers.GetWorkingDirectory(context, prompt.WorkingDirectoryId, currentUser.UserId);

        prompt.Title = request.Title.Trim();
        prompt.Content = request.Content;
        prompt.TargetAgent = request.TargetAgent;
        prompt.Kind = request.Kind;
        prompt.Status = request.Status;
        prompt.CurrentVersion++;
        var pausedLinkedDocuments = PromptMutationHelpers.PauseLinkedDocumentsIfPromptIsArchived(
            context,
            prompt,
            dateTimeProvider);

        var existingReferences = context.PromptFileReferences
            .Where(reference => reference.PromptId == prompt.Id)
            .ToList();
        context.RemoveRange(existingReferences);

        var references = await PromptMutationHelpers.BuildReferencesAsync(
            workspaceFileService,
            directory.AbsolutePath,
            request.Mentions,
            cancellationToken);

        foreach (var reference in references)
        {
            reference.PromptId = prompt.Id;
        }

        context.Add(PromptMutationHelpers.CreateVersion(prompt, dateTimeProvider, "Updated"));
        context.AddRange(references);

        await context.SaveChangesAsync(cancellationToken);

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
