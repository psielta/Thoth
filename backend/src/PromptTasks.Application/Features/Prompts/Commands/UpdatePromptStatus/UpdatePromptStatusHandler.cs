using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Features.Prompts;

namespace PromptTasks.Application.Features.Prompts.Commands.UpdatePromptStatus;

public sealed class UpdatePromptStatusHandler(
    IApplicationDbContext context,
    IPromptNotifier promptNotifier,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdatePromptStatusCommand, PromptDto>
{
    public async Task<PromptDto> Handle(UpdatePromptStatusCommand request, CancellationToken cancellationToken)
    {
        var prompt = PromptMutationHelpers.GetPrompt(context, request.Id, currentUser.UserId);
        PromptMutationHelpers.EnsureRowVersion(prompt, request.RowVersion);

        prompt.Status = request.Status;
        prompt.CurrentVersion++;

        context.Add(PromptMutationHelpers.CreateVersion(prompt, dateTimeProvider, "Status changed"));
        await context.SaveChangesAsync(cancellationToken);

        var references = context.PromptFileReferences
            .Where(reference => reference.PromptId == prompt.Id)
            .ToList();
        var dto = prompt.ToDto(references);
        await promptNotifier.PromptUpdatedAsync(dto, cancellationToken);
        return dto;
    }
}
