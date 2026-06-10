using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Interfaces;

public interface IGitCommandRunner
{
    Task<GitCommandResult> RunAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken);
}
