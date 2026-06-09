using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.WorkingDirectories.Queries.ValidateWorkingDirectoryPath;

public sealed class ValidateWorkingDirectoryPathHandler(IWorkspaceFileService workspaceFileService)
    : IRequestHandler<ValidateWorkingDirectoryPathQuery, ValidatePathResponse>
{
    public async Task<ValidatePathResponse> Handle(
        ValidateWorkingDirectoryPathQuery request,
        CancellationToken cancellationToken)
    {
        var result = await workspaceFileService.ValidatePathAsync(request.AbsolutePath, cancellationToken);
        return new ValidatePathResponse(result.IsValid, result.CanonicalPath, result.Error);
    }
}
