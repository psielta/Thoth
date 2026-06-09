using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.FutureTasks.Queries.GetFutureTask;

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
