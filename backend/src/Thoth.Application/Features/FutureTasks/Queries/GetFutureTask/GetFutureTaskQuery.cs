using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.FutureTasks.Queries.GetFutureTask;

public sealed record GetFutureTaskQuery(Guid Id) : IRequest<FutureTaskDto>;
