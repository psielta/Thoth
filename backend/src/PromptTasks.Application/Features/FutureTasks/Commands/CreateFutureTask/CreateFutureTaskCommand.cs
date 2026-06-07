using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.FutureTasks;

namespace PromptTasks.Application.Features.FutureTasks.Commands.CreateFutureTask;

public sealed record CreateFutureTaskCommand(
    Guid WorkingDirectoryId,
    string Title,
    string Description,
    FutureTaskType Type,
    IReadOnlyList<string>? Labels,
    string? IssueGithubId) : IRequest<FutureTaskDto>;
