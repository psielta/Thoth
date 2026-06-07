using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.FutureTasks;

namespace PromptTasks.Application.Features.FutureTasks.Commands.UpdateFutureTask;

public sealed record UpdateFutureTaskCommand(
    Guid Id,
    string Title,
    string Description,
    FutureTaskType Type,
    IReadOnlyList<string>? Labels,
    string? IssueGithubId,
    string RowVersion) : IRequest<FutureTaskDto>;
