using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Thoth.Application.Common.Models;

namespace Thoth.Api.IntegrationTests;

public sealed class GitApiTests(ThothApiFactory factory) : IClassFixture<ThothApiFactory>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"prompttasks-git-api-{Guid.NewGuid():N}");

    [Fact]
    public async Task Git_endpoints_return_status_original_content_and_diff()
    {
        SkipIfGitUnavailable();
        Directory.CreateDirectory(Path.Combine(_tempRoot, "src"));
        await RunGit("init");
        await RunGit("config", "user.name", "Test");
        await RunGit("config", "user.email", "test@example.com");
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "src", "app.txt"), "original app");
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "old-name.txt"), "rename me");
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "deleted.txt"), "remove me");
        await RunGit("add", ".");
        await RunGit("-c", "commit.gpgsign=false", "commit", "-m", "init");
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "src", "app.txt"), "changed app");
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "untracked.txt"), "new content");
        File.Delete(Path.Combine(_tempRoot, "deleted.txt"));
        await RunGit("mv", "old-name.txt", "new-name.txt");

        var client = factory.CreateClient();
        var wd = await CreateWorkingDirectory(client, _tempRoot);

        var status = await client.GetFromJsonAsync<GitFileStatusDto[]>(
            $"/api/git/status?workingDirectoryId={wd.Id}",
            JsonOptions);
        status.Should().Contain(item => item.Path == "src/app.txt" && item.Status == GitFileChangeStatus.Modified);
        status.Should().Contain(item => item.Path == "untracked.txt" && item.Status == GitFileChangeStatus.Untracked);
        status.Should().Contain(item => item.Path == "deleted.txt" && item.Status == GitFileChangeStatus.Deleted);
        status.Should().Contain(item =>
            item.Path == "new-name.txt" &&
            item.OriginalPath == "old-name.txt" &&
            item.Status == GitFileChangeStatus.Renamed);

        var original = await client.GetFromJsonAsync<GitOriginalFileDto>(
            $"/api/git/original-file?workingDirectoryId={wd.Id}&path={Uri.EscapeDataString("src/app.txt")}",
            JsonOptions);
        original!.Content.Should().Be("original app");

        var untrackedOriginal = await client.GetFromJsonAsync<GitOriginalFileDto>(
            $"/api/git/original-file?workingDirectoryId={wd.Id}&path={Uri.EscapeDataString("untracked.txt")}",
            JsonOptions);
        untrackedOriginal!.Content.Should().BeEmpty();

        var diff = await client.GetFromJsonAsync<GitDiffDto>(
            $"/api/git/diff?workingDirectoryId={wd.Id}&path={Uri.EscapeDataString("src/app.txt")}",
            JsonOptions);
        diff!.Diff.Should().Contain("diff --git");
        diff.Diff.Should().Contain("src/app.txt");
    }

    [Fact]
    public async Task Git_status_returns_empty_for_non_repo_directory()
    {
        Directory.CreateDirectory(_tempRoot);
        var client = factory.CreateClient();
        var wd = await CreateWorkingDirectory(client, _tempRoot);

        var status = await client.GetFromJsonAsync<GitFileStatusDto[]>(
            $"/api/git/status?workingDirectoryId={wd.Id}",
            JsonOptions);

        status.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (!Directory.Exists(_tempRoot))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(_tempRoot, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(_tempRoot, recursive: true);
    }

    private static async Task<WorkingDirectoryDto> CreateWorkingDirectory(HttpClient client, string absolutePath)
    {
        var response = await client.PostAsJsonAsync(
            "/api/working-directories",
            new { name = $"repo-{Guid.NewGuid():N}", absolutePath, respectGitignore = true },
            JsonOptions);
        response.EnsureSuccessStatusCode();
        var workingDirectory = await response.Content.ReadFromJsonAsync<WorkingDirectoryDto>(JsonOptions);
        return workingDirectory!;
    }

    private async Task RunGit(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _tempRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        process.ExitCode.Should().Be(0, error);
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
