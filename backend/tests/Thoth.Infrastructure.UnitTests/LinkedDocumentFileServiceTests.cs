using FluentAssertions;
using Microsoft.Extensions.Options;
using Thoth.Infrastructure.FileSystem;

namespace Thoth.Infrastructure.UnitTests;

public sealed class LinkedDocumentFileServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"prompttasks-linked-doc-{Guid.NewGuid():N}");
    private readonly LinkedDocumentFileService _service;

    public LinkedDocumentFileServiceTests()
    {
        Directory.CreateDirectory(_root);
        _service = new LinkedDocumentFileService(Options.Create(new LinkedDocumentOptions
        {
            MaxFileSizeBytes = 1024
        }));
    }

    [Fact]
    public async Task Validate_accepts_markdown_and_returns_normalized_key()
    {
        var path = Path.Combine(_root, "Plan.md");
        await File.WriteAllTextAsync(path, "# Plan");

        var result = await _service.ValidateAsync(path, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.CanonicalPath.Should().Be(Path.GetFullPath(path));
        result.PathKey.Should().Be(Path.GetFullPath(path).ToLowerInvariant());
    }

    [Fact]
    public async Task Validate_rejects_non_markdown_files()
    {
        var path = Path.Combine(_root, "plan.txt");
        await File.WriteAllTextAsync(path, "not markdown");

        var result = await _service.ValidateAsync(path, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Only .md and .markdown files can be linked.");
    }

    [Fact]
    public async Task Read_returns_content_hash_and_size()
    {
        var path = Path.Combine(_root, "plan.md");
        await File.WriteAllTextAsync(path, "# Plan");

        var first = await _service.ReadAsync(path, CancellationToken.None);
        var second = await _service.ReadAsync(path, CancellationToken.None);

        first.Success.Should().BeTrue();
        first.Content.Should().Be("# Plan");
        first.ContentHash.Should().Be(second.ContentHash);
        first.SizeBytes.Should().Be(new FileInfo(path).Length);
    }

    [Fact]
    public async Task Read_marks_missing_files()
    {
        var result = await _service.ReadAsync(Path.Combine(_root, "missing.md"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FileMissing.Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
