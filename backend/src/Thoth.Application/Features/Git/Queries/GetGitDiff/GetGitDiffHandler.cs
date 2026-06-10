using MediatR;
using Microsoft.Extensions.Logging;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Git.Queries.GetGitDiff;

public sealed class GetGitDiffHandler(
    IApplicationDbContext context,
    IGitCommandRunner git,
    ICurrentUser currentUser,
    ILogger<GetGitDiffHandler> logger)
    : IRequestHandler<GetGitDiffQuery, GitDiffDto>
{
    public async Task<GitDiffDto> Handle(GetGitDiffQuery request, CancellationToken cancellationToken)
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
            [
                "-c",
                "core.quotepath=false",
                "diff",
                "HEAD",
                "--",
                normalizedPath
            ],
            cancellationToken);

        if (result.ExitCode is 0 or 1)
        {
            return new GitDiffDto(result.StandardOutput);
        }

        logger.LogDebug(
            "Git diff failed for {Path} in {WorkingDirectory}. Exit code: {ExitCode}; stderr: {StandardError}",
            normalizedPath,
            directory.AbsolutePath,
            result.ExitCode,
            result.StandardError);
        return new GitDiffDto(string.Empty);
    }
}
