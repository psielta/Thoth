using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.FutureTasks;

namespace PromptTasks.Application.Features.FutureTasks.Queries.GetFutureTasks;

public sealed record GetFutureTasksQuery(
    Guid? WorkingDirectoryId,
    FutureTaskStatus? Status,
    bool IncludeArchived,
    FutureTaskType? Type,
    string? Label,
    string? Q) : IRequest<IReadOnlyList<FutureTaskDto>>;
