using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.FutureTasks.Queries.GetFutureTask;

public sealed record GetFutureTaskQuery(Guid Id) : IRequest<FutureTaskDto>;
