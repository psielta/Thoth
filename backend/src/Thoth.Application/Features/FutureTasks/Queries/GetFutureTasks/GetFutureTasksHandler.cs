using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Domain.FutureTasks;

namespace Thoth.Application.Features.FutureTasks.Queries.GetFutureTasks;

public sealed class GetFutureTasksHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetFutureTasksQuery, IReadOnlyList<FutureTaskDto>>
{
    public Task<IReadOnlyList<FutureTaskDto>> Handle(GetFutureTasksQuery request, CancellationToken cancellationToken)
    {
        var query = context.FutureTasks.Where(task => task.OwnerId == currentUser.UserId);

        if (request.WorkingDirectoryId is { } workingDirectoryId)
        {
            query = query.Where(task => task.WorkingDirectoryId == workingDirectoryId);
        }

        if (request.Status is { } status)
        {
            query = query.Where(task => task.Status == status);
        }
        else if (!request.IncludeArchived)
        {
            query = query.Where(task => task.Status != FutureTaskStatus.Archived);
        }

        if (request.Type is { } type)
        {
            query = query.Where(task => task.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(request.Label))
        {
            var label = request.Label.Trim();
            query = query.Where(task => task.Labels.Any(item => item.Label == label));
        }

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var term = request.Q.Trim();
            query = query.Where(task => task.Title.Contains(term) || task.Description.Contains(term));
        }

        var tasks = query
            .OrderByDescending(task => task.UpdatedAtUtc)
            .ThenBy(task => task.Title)
            .ToList();

        var taskIds = tasks.Select(task => task.Id).ToHashSet();

        var labels = context.FutureTaskLabels
            .Where(label => taskIds.Contains(label.FutureTaskId))
            .ToList()
            .GroupBy(label => label.FutureTaskId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Label).AsEnumerable());

        var counts = context.Prompts
            .Where(prompt => prompt.FutureTaskId.HasValue && taskIds.Contains(prompt.FutureTaskId.Value))
            .Select(prompt => prompt.FutureTaskId!.Value)
            .ToList()
            .GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());

        IReadOnlyList<FutureTaskDto> result = tasks
            .Select(task => task.ToDto(
                labels.GetValueOrDefault(task.Id) ?? Enumerable.Empty<string>(),
                counts.GetValueOrDefault(task.Id)))
            .ToList();

        return Task.FromResult(result);
    }
}
