using MediatR;

namespace Thoth.Application.Features.FutureTasks.Commands.DeleteFutureTask;

public sealed record DeleteFutureTaskCommand(Guid Id) : IRequest;
