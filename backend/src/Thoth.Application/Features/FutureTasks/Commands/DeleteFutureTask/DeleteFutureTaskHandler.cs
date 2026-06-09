using MediatR;
using Thoth.Application.Common.Interfaces;

namespace Thoth.Application.Features.FutureTasks.Commands.DeleteFutureTask;

public sealed class DeleteFutureTaskHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<DeleteFutureTaskCommand>
{
    public async Task Handle(DeleteFutureTaskCommand request, CancellationToken cancellationToken)
    {
        var task = FutureTaskMutationHelpers.GetFutureTask(context, request.Id, currentUser.UserId);

        context.Remove(task);
        await context.SaveChangesAsync(cancellationToken);
    }
}
