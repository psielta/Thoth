using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Features.Prompts;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.Prompts.Commands.CreatePrompt;

public sealed class CreatePromptHandler(
    IApplicationDbContext context,
    IWorkspaceFileService workspaceFileService,
    IPromptNotifier promptNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CreatePromptCommand, PromptDto>
{
    public async Task<PromptDto> Handle(CreatePromptCommand request, CancellationToken cancellationToken)
    {
        var directory = PromptMutationHelpers.GetWorkingDirectory(context, request.WorkingDirectoryId, currentUser.UserId);

        var prompt = new Prompt
        {
            WorkingDirectoryId = directory.Id,
            Title = request.Title.Trim(),
            Content = request.Content,
            TargetAgent = request.TargetAgent,
            Kind = request.Kind,
            Status = request.Status,
            CurrentVersion = 1,
            OwnerId = currentUser.UserId
        };

        var references = await PromptMutationHelpers.BuildReferencesAsync(
            workspaceFileService,
            directory.AbsolutePath,
            request.Mentions,
            cancellationToken);

        foreach (var reference in references)
        {
            reference.PromptId = prompt.Id;
        }

        context.Add(prompt);
        context.Add(PromptMutationHelpers.CreateVersion(prompt, dateTimeProvider, "Created"));
        context.AddRange(references);

        await context.SaveChangesAsync(cancellationToken);

        var dto = prompt.ToDto(references);
        await promptNotifier.PromptCreatedAsync(dto, cancellationToken);
        return dto;
    }
}
