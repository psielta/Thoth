using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.FutureTasks.Commands.UpdateFutureTaskStatus;

public sealed class UpdateFutureTaskStatusHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<UpdateFutureTaskStatusCommand, FutureTaskDto>
{
    public async Task<FutureTaskDto> Handle(UpdateFutureTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = FutureTaskMutationHelpers.GetFutureTask(context, request.Id, currentUser.UserId);
        FutureTaskMutationHelpers.EnsureRowVersion(task, request.RowVersion);

        task.Status = request.Status;
        await context.SaveChangesAsync(cancellationToken);

        return task.ToDto(
            FutureTaskMutationHelpers.LoadLabels(context, task.Id),
            FutureTaskMutationHelpers.CountLinkedPrompts(context, task.Id));
    }
}
