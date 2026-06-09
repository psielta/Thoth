using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.FutureTasks;

namespace Thoth.Application.Features.FutureTasks.Commands.UpdateFutureTaskStatus;

public sealed record UpdateFutureTaskStatusCommand(
    Guid Id,
    FutureTaskStatus Status,
    string RowVersion) : IRequest<FutureTaskDto>;
