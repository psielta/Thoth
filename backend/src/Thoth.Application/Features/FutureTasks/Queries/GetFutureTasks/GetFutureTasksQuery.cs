using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.FutureTasks;

namespace Thoth.Application.Features.FutureTasks.Queries.GetFutureTasks;

public sealed record GetFutureTasksQuery(
    Guid? WorkingDirectoryId,
    FutureTaskStatus? Status,
    bool IncludeArchived,
    FutureTaskType? Type,
    string? Label,
    string? Q) : IRequest<IReadOnlyList<FutureTaskDto>>;
