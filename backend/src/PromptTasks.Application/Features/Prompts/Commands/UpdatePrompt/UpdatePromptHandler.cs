using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Features.Prompts;

namespace PromptTasks.Application.Features.Prompts.Commands.UpdatePrompt;

public sealed class UpdatePromptHandler(
    IApplicationDbContext context,
    IWorkspaceFileService workspaceFileService,
    IPromptNotifier promptNotifier,
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
        return dto;
    }
}
