using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.WorkingDirectories.Queries.GetWorkingDirectory;

public sealed class GetWorkingDirectoryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetWorkingDirectoryQuery, WorkingDirectoryDto>
{
    public Task<WorkingDirectoryDto> Handle(GetWorkingDirectoryQuery request, CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        return Task.FromResult(directory.ToDto());
    }
}
