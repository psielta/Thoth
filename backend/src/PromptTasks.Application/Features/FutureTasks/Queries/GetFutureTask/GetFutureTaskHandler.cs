using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.FutureTasks.Queries.GetFutureTask;

public sealed class GetFutureTaskHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetFutureTaskQuery, FutureTaskDto>
{
    public Task<FutureTaskDto> Handle(GetFutureTaskQuery request, CancellationToken cancellationToken)
    {
        var task = FutureTaskMutationHelpers.GetFutureTask(context, request.Id, currentUser.UserId);

        return Task.FromResult(task.ToDto(
            FutureTaskMutationHelpers.LoadLabels(context, task.Id),
            FutureTaskMutationHelpers.CountLinkedPrompts(context, task.Id)));
    }
}
