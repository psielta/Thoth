using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;

namespace PromptTasks.Application.Features.WorkingDirectories.Queries.GetWorkingDirectories;

public sealed class GetWorkingDirectoriesHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetWorkingDirectoriesQuery, IReadOnlyList<Common.Models.WorkingDirectoryDto>>
{
    public Task<IReadOnlyList<Common.Models.WorkingDirectoryDto>> Handle(
        GetWorkingDirectoriesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Common.Models.WorkingDirectoryDto> result = context.WorkingDirectories
            .Where(directory => directory.OwnerId == currentUser.UserId)
            .OrderBy(directory => directory.Name)
            .ThenBy(directory => directory.AbsolutePath)
            .Select(directory => directory.ToDto())
            .ToList();

        return Task.FromResult(result);
    }
}
