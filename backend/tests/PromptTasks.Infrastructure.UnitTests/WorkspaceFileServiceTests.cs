using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Infrastructure.FileSystem;

namespace PromptTasks.Infrastructure.UnitTests;

public sealed class WorkspaceFileServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"prompttasks-{Guid.NewGuid():N}");
    private readonly WorkspaceFileService _service;

    public WorkspaceFileServiceTests()
    {
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        Directory.CreateDirectory(Path.Combine(_root, "node_modules"));
        File.WriteAllText(Path.Combine(_root, "src", "main.go"), "package main");
        File.WriteAllText(Path.Combine(_root, "node_modules", "main.js"), "ignored");
        _service = new WorkspaceFileService(new MemoryCache(new MemoryCacheOptions()), new FakeDateTimeProvider());
    }

    [Fact]
    public async Task Search_returns_ranked_internal_paths_and_prunes_ignored_directories()
    {
        var result = await _service.SearchAsync(Guid.NewGuid(), _root, "main", 50, false, CancellationToken.None);

        result.Should().Contain(item => item.RelativePath == "src/main.go");
        result.Should().NotContain(item => item.RelativePath.StartsWith("node_modules/", StringComparison.OrdinalIgnoreCase));
        result.Should().OnlyContain(item => item.RelativePath.Contains('/'));
    }

    [Fact]
    public async Task Search_accepts_leading_mention_marker()
    {
        var result = await _service.SearchAsync(Guid.NewGuid(), _root, "@main.go", 50, false, CancellationToken.None);

        result.Should().ContainSingle(item => item.RelativePath == "src/main.go");
    }

    [Fact]
    public async Task ResolveRelativePath_rejects_parent_traversal()
    {
        var act = () => _service.ResolveRelativePathAsync(_root, "../outside.txt", CancellationToken.None);

        await act.Should().ThrowAsync<PathTraversalException>();
    }

    [Fact]
    public async Task ValidateRelativePaths_marks_existing_and_missing_paths()
    {
        var result = await _service.ValidateRelativePathsAsync(
            _root,
            new[] { "@src/main.go", "src/missing.go" },
            CancellationToken.None);

        result.Should().ContainSingle(item =>
            item.RawPath == "@src/main.go" &&
            item.RelativePath == "src/main.go" &&
            item.Exists &&
            !item.IsDirectory);
        result.Should().ContainSingle(item =>
            item.RelativePath == "src/missing.go" &&
            !item.Exists &&
            item.Error == "File or directory was not found.");
    }

    [Fact]
    public async Task ValidateRelativePaths_reports_parent_traversal_without_throwing()
    {
        var result = await _service.ValidateRelativePathsAsync(_root, new[] { "../outside.txt" }, CancellationToken.None);

        result.Should().ContainSingle(item =>
            item.RawPath == "../outside.txt" &&
            !item.Exists &&
            item.Error == "Only relative paths inside the working directory are allowed.");
    }

    [Fact]
    public async Task ValidatePath_rejects_files()
    {
        var filePath = Path.Combine(_root, "src", "main.go");

        var result = await _service.ValidatePathAsync(filePath, CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveRelativePath_rejects_symlink_escape_when_supported()
    {
        var outside = Path.Combine(Path.GetTempPath(), $"prompttasks-outside-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "secret.txt"), "secret");
        var link = Path.Combine(_root, "escape");

        try
        {
            Directory.CreateSymbolicLink(link, outside);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            Directory.Delete(outside, recursive: true);
            return;
        }

        try
        {
            var act = () => _service.ResolveRelativePathAsync(_root, "escape/secret.txt", CancellationToken.None);
            await act.Should().ThrowAsync<PathTraversalException>();
        }
        finally
        {
            Directory.Delete(outside, recursive: true);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
    }
}
