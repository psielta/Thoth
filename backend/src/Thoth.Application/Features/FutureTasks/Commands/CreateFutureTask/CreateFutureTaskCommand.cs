using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.FutureTasks;

namespace Thoth.Application.Features.FutureTasks.Commands.CreateFutureTask;

public sealed record CreateFutureTaskCommand(
    Guid WorkingDirectoryId,
    string Title,
    string Description,
    FutureTaskType Type,
    IReadOnlyList<string>? Labels,
    string? IssueGithubId) : IRequest<FutureTaskDto>;
