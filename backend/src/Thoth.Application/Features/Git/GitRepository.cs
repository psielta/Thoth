using Microsoft.Extensions.Logging;
using Thoth.Application.Common.Interfaces;

namespace Thoth.Application.Features.Git;

public static class GitRepository
{
    public static async Task<GitRepositoryProbe> ProbeAsync(
        IGitCommandRunner git,
        string workingDirectory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var result = await git.RunAsync(
            workingDirectory,
            ["rev-parse", "--show-prefix"],
            cancellationToken);

        if (result.ExitCode != 0)
        {
            logger.LogDebug(
                "Working directory {WorkingDirectory} is not inside a Git repository. Exit code: {ExitCode}; stderr: {StandardError}",
                workingDirectory,
                result.ExitCode,
                result.StandardError);
            return new GitRepositoryProbe(false, string.Empty);
        }

        return new GitRepositoryProbe(true, NormalizePrefix(result.StandardOutput));
    }

    private static string NormalizePrefix(string prefix) =>
        prefix.Trim().Replace('\\', '/').Trim('/');
}

public sealed record GitRepositoryProbe(bool IsRepository, string Prefix);
