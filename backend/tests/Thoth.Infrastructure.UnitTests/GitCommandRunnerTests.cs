using System.ComponentModel;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Thoth.Infrastructure.Git;

namespace Thoth.Infrastructure.UnitTests;

public sealed class GitCommandRunnerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"prompttasks-{Guid.NewGuid():N}");

    public GitCommandRunnerTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task RunAsync_git_version_returns_exit_zero()
    {
        SkipIfGitUnavailable();
        var runner = CreateRunner();

        var result = await runner.RunAsync(_root, ["--version"], CancellationToken.None);

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("git version");
    }

    [Fact]
    public async Task RunAsync_status_in_non_repo_returns_non_zero_with_stderr()
    {
        SkipIfGitUnavailable();
        var runner = CreateRunner();

        var result = await runner.RunAsync(_root, ["status"], CancellationToken.None);

        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RunAsync_reads_porcelain_status_and_original_content_from_real_repo()
    {
        SkipIfGitUnavailable();
        var runner = CreateRunner();
        await RunGit(runner, ["init"]);
        await File.WriteAllTextAsync(Path.Combine(_root, "arquivo.txt"), "versao inicial");
        await RunGit(runner, ["add", "."]);
        await RunGit(runner, ["-c", "user.name=Test", "-c", "user.email=test@example.com", "-c", "commit.gpgsign=false", "commit", "-m", "init"]);
        await File.WriteAllTextAsync(Path.Combine(_root, "arquivo.txt"), "versao alterada");

        var status = await RunGit(runner, ["-c", "core.quotepath=false", "status", "--porcelain=v1", "-z", "--", "."]);
        var original = await RunGit(runner, ["show", "HEAD:./arquivo.txt"]);

        status.StandardOutput.Should().Contain(" M arquivo.txt");
        original.StandardOutput.Should().Be("versao inicial");
    }

    [Fact]
    public async Task RunAsync_pre_canceled_token_throws()
    {
        SkipIfGitUnavailable();
        var runner = CreateRunner();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => runner.RunAsync(_root, ["--version"], cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose()
    {
        if (!Directory.Exists(_root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(_root, recursive: true);
    }

    private static GitCommandRunner CreateRunner() =>
        new(NullLogger<GitCommandRunner>.Instance);

    private async Task<Thoth.Application.Common.Models.GitCommandResult> RunGit(
        GitCommandRunner runner,
        IReadOnlyList<string> arguments,
        bool assertSuccess = true)
    {
        var result = await runner.RunAsync(_root, arguments, CancellationToken.None);
        if (assertSuccess)
        {
            result.ExitCode.Should().Be(0, result.StandardError);
        }

        return result;
    }

    private static void SkipIfGitUnavailable()
    {
        if (!GitAvailable.Value)
        {
            throw Xunit.Sdk.SkipException.ForSkip("Git executable is not available on this machine.");
        }
    }

    private static readonly Lazy<bool> GitAvailable = new(() =>
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--version");
            using var process = Process.Start(startInfo);
            process?.WaitForExit(5_000);
            return process?.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    });
}
