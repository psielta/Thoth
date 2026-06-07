using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.FutureTasks;

namespace PromptTasks.Application.Features.FutureTasks.Commands.CreateFutureTask;

public sealed class CreateFutureTaskHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<CreateFutureTaskCommand, FutureTaskDto>
{
    public async Task<FutureTaskDto> Handle(CreateFutureTaskCommand request, CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.WorkingDirectoryId && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        var labels = FutureTaskMutationHelpers.NormalizeLabels(request.Labels);

        var task = new FutureTask
        {
            WorkingDirectoryId = directory.Id,
            Title = request.Title.Trim(),
            Description = request.Description,
            Status = FutureTaskStatus.Open,
            Type = request.Type,
            IssueGithubId = FutureTaskMutationHelpers.NormalizeIssueGithubId(request.IssueGithubId),
            OwnerId = currentUser.UserId
        };

        foreach (var label in labels)
        {
            task.Labels.Add(new FutureTaskLabel { FutureTaskId = task.Id, Label = label });
        }

        context.Add(task);
        await context.SaveChangesAsync(cancellationToken);

        return task.ToDto(labels, 0);
    }
}
