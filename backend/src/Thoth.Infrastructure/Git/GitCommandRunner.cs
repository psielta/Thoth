using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Infrastructure.Git;

public sealed class GitCommandRunner(ILogger<GitCommandRunner> logger) : IGitCommandRunner
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);

    public async Task<GitCommandResult> RunAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(CommandTimeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeout.Token);
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogDebug(
                "Running git command in {WorkingDirectory}: {Arguments}",
                workingDirectory,
                string.Join(" ", arguments));

            if (!process.Start())
            {
                logger.LogWarning("Git process failed to start in {WorkingDirectory}.", workingDirectory);
                return new GitCommandResult(-1, string.Empty, "Git process failed to start.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            try
            {
                await process.WaitForExitAsync(linkedCancellation.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
            {
                KillProcess(process);
                var timedOutOutput = await outputTask;
                var timedOutError = await errorTask;
                logger.LogWarning(
                    "Git command timed out after {ElapsedMilliseconds} ms in {WorkingDirectory}: {Arguments}",
                    stopwatch.ElapsedMilliseconds,
                    workingDirectory,
                    string.Join(" ", arguments));
                return new GitCommandResult(
                    -1,
                    timedOutOutput,
                    string.IsNullOrWhiteSpace(timedOutError) ? "Git command timed out." : timedOutError);
            }
            catch (OperationCanceledException)
            {
                KillProcess(process);
                throw;
            }

            var output = await outputTask;
            var error = await errorTask;
            logger.LogDebug(
                "Git command finished in {ElapsedMilliseconds} ms with exit code {ExitCode} in {WorkingDirectory}.",
                stopwatch.ElapsedMilliseconds,
                process.ExitCode,
                workingDirectory);

            if (!string.IsNullOrWhiteSpace(error))
            {
                logger.LogDebug("Git stderr: {StandardError}", error);
            }

            return new GitCommandResult(process.ExitCode, output, error);
        }
        catch (Win32Exception exception)
        {
            logger.LogWarning(
                exception,
                "Git executable was not available while running in {WorkingDirectory}.",
                workingDirectory);
            return new GitCommandResult(-1, string.Empty, exception.Message);
        }
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
