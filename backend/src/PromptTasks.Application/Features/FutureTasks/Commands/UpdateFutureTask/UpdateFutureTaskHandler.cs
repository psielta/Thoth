using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.FutureTasks;

namespace PromptTasks.Application.Features.FutureTasks.Commands.UpdateFutureTask;

public sealed class UpdateFutureTaskHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<UpdateFutureTaskCommand, FutureTaskDto>
{
    public async Task<FutureTaskDto> Handle(UpdateFutureTaskCommand request, CancellationToken cancellationToken)
    {
        var task = FutureTaskMutationHelpers.GetFutureTask(context, request.Id, currentUser.UserId);
        FutureTaskMutationHelpers.EnsureRowVersion(task, request.RowVersion);

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Type = request.Type;
        task.IssueGithubId = FutureTaskMutationHelpers.NormalizeIssueGithubId(request.IssueGithubId);

        var desired = FutureTaskMutationHelpers.NormalizeLabels(request.Labels);
        var existing = context.FutureTaskLabels
            .Where(label => label.FutureTaskId == task.Id)
            .ToList();

        var toRemove = existing
            .Where(label => !desired.Contains(label.Label, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var existingValues = existing
            .Select(label => label.Label)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toAdd = desired
            .Where(label => !existingValues.Contains(label))
            .ToList();

        context.RemoveRange(toRemove);
        foreach (var label in toAdd)
        {
            context.Add(new FutureTaskLabel { FutureTaskId = task.Id, Label = label });
        }

        await context.SaveChangesAsync(cancellationToken);

        return task.ToDto(
            FutureTaskMutationHelpers.LoadLabels(context, task.Id),
            FutureTaskMutationHelpers.CountLinkedPrompts(context, task.Id));
    }
}
