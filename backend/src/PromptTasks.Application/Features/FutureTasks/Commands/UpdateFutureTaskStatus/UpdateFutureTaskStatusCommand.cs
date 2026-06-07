using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.FutureTasks;

namespace PromptTasks.Application.Features.FutureTasks.Commands.UpdateFutureTaskStatus;

public sealed record UpdateFutureTaskStatusCommand(
    Guid Id,
    FutureTaskStatus Status,
    string RowVersion) : IRequest<FutureTaskDto>;
