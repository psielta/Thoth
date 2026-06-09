using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.FutureTasks;

namespace Thoth.Application.Features.FutureTasks.Commands.UpdateFutureTask;

public sealed record UpdateFutureTaskCommand(
    Guid Id,
    string Title,
    string Description,
    FutureTaskType Type,
    IReadOnlyList<string>? Labels,
    string? IssueGithubId,
    string RowVersion) : IRequest<FutureTaskDto>;
