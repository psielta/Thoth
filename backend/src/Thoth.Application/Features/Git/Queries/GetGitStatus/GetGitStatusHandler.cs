using MediatR;
using Microsoft.Extensions.Logging;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Git.Queries.GetGitStatus;

public sealed class GetGitStatusHandler(
    IApplicationDbContext context,
    IGitCommandRunner git,
    ICurrentUser currentUser,
    ILogger<GetGitStatusHandler> logger)
    : IRequestHandler<GetGitStatusQuery, IReadOnlyList<GitFileStatusDto>>
{
    public async Task<IReadOnlyList<GitFileStatusDto>> Handle(
        GetGitStatusQuery request,
        CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.WorkingDirectoryId && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        var probe = await GitRepository.ProbeAsync(git, directory.AbsolutePath, logger, cancellationToken);
        if (!probe.IsRepository)
        {
            return Array.Empty<GitFileStatusDto>();
        }

        var result = await git.RunAsync(
            directory.AbsolutePath,
            [
                "-c",
                "core.quotepath=false",
                "--no-optional-locks",
                "status",
                "--porcelain=v1",
                "-z",
                "--untracked-files=all",
                "--",
                "."
            ],
            cancellationToken);

        if (result.ExitCode != 0)
        {
            logger.LogDebug(
                "Git status failed for working directory {WorkingDirectory}. Exit code: {ExitCode}; stderr: {StandardError}",
                directory.AbsolutePath,
                result.ExitCode,
                result.StandardError);
            return Array.Empty<GitFileStatusDto>();
        }

        return GitPorcelainParser.Parse(result.StandardOutput)
            .Select(item => StripPrefix(item, probe.Prefix))
            .Where(item => item.Path.Length > 0)
            .ToArray();
    }

    private static GitFileStatusDto StripPrefix(GitFileStatusDto item, string prefix) =>
        item with
        {
            Path = StripPrefix(item.Path, prefix),
            OriginalPath = item.OriginalPath is null ? null : StripPrefix(item.OriginalPath, prefix)
        };

    private static string StripPrefix(string path, string prefix)
    {
        var normalizedPath = path.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return normalizedPath;
        }

        var normalizedPrefix = prefix.Trim().Replace('\\', '/').Trim('/');
        if (normalizedPrefix.Length == 0)
        {
            return normalizedPath;
        }

        if (normalizedPath.Equals(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var prefixWithSlash = normalizedPrefix + "/";
        return normalizedPath.StartsWith(prefixWithSlash, StringComparison.OrdinalIgnoreCase)
            ? normalizedPath[prefixWithSlash.Length..]
            : normalizedPath;
    }
}
