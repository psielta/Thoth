using MediatR;
using Microsoft.Extensions.Logging;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Git.Queries.GetOriginalFileContent;

public sealed class GetOriginalFileContentHandler(
    IApplicationDbContext context,
    IGitCommandRunner git,
    ICurrentUser currentUser,
    ILogger<GetOriginalFileContentHandler> logger)
    : IRequestHandler<GetOriginalFileContentQuery, GitOriginalFileDto>
{
    public async Task<GitOriginalFileDto> Handle(
        GetOriginalFileContentQuery request,
        CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.WorkingDirectoryId && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        var normalizedPath = GitRelativePath.Normalize(request.Path);
        var result = await git.RunAsync(
            directory.AbsolutePath,
            ["show", $"HEAD:./{normalizedPath}"],
            cancellationToken);

        if (result.ExitCode == 0)
        {
            return new GitOriginalFileDto(result.StandardOutput);
        }

        logger.LogDebug(
            "Original Git content was unavailable for {Path} in {WorkingDirectory}. Exit code: {ExitCode}; stderr: {StandardError}",
            normalizedPath,
            directory.AbsolutePath,
            result.ExitCode,
            result.StandardError);
        return new GitOriginalFileDto(string.Empty);
    }
}
