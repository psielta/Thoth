using MediatR;

namespace PromptTasks.Application.Features.FutureTasks.Commands.DeleteFutureTask;

public sealed record DeleteFutureTaskCommand(Guid Id) : IRequest;
