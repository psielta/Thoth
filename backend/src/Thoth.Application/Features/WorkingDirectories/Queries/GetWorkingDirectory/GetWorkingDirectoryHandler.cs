using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.WorkingDirectories.Queries.GetWorkingDirectory;

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
