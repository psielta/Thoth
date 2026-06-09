using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Infrastructure.FileSystem;

public sealed class LinkedDocumentFileService(IOptions<LinkedDocumentOptions> options) : ILinkedDocumentFileService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md",
        ".markdown"
    };

    public Task<MarkdownFileValidation> ValidateAsync(string absolutePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return Task.FromResult(MarkdownFileValidation.Invalid("Path is required."));
        }

        try
        {
            if (!Path.IsPathFullyQualified(absolutePath))
            {
                return Task.FromResult(MarkdownFileValidation.Invalid("Path must be absolute."));
            }

            if (!options.Value.AllowUncPaths && absolutePath.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return Task.FromResult(MarkdownFileValidation.Invalid("UNC paths are not allowed."));
            }

            if (!AllowedExtensions.Contains(Path.GetExtension(absolutePath)))
            {
                return Task.FromResult(MarkdownFileValidation.Invalid("Only .md and .markdown files can be linked."));
            }

            if (!File.Exists(absolutePath))
            {
                return Task.FromResult(MarkdownFileValidation.Invalid("Markdown file was not found."));
            }

            if (Directory.Exists(absolutePath))
            {
                return Task.FromResult(MarkdownFileValidation.Invalid("Path must point to a markdown file."));
            }

            var canonicalPath = LinkedDocumentPath.CanonicalizeExistingPath(absolutePath);
            var info = new FileInfo(canonicalPath);
            if (info.Length > options.Value.MaxFileSizeBytes)
            {
                return Task.FromResult(MarkdownFileValidation.Invalid(
                    $"Markdown file must be {options.Value.MaxFileSizeBytes} bytes or smaller."));
            }

            using var stream = OpenReadShared(canonicalPath);
            return Task.FromResult(MarkdownFileValidation.Valid(
                canonicalPath,
                LinkedDocumentPath.CreateKey(canonicalPath),
                info.Length));
        }
        catch (Exception exception) when (exception is IOException
                                            or UnauthorizedAccessException
                                            or ArgumentException
                                            or NotSupportedException)
        {
            return Task.FromResult(MarkdownFileValidation.Invalid(exception.Message));
        }
    }

    public async Task<MarkdownFileReadResult> ReadAsync(string absolutePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (!File.Exists(absolutePath))
            {
                return MarkdownFileReadResult.Missing("Markdown file was not found.");
            }

            var canonicalPath = LinkedDocumentPath.CanonicalizeExistingPath(absolutePath);
            var info = new FileInfo(canonicalPath);
            if (info.Length > options.Value.MaxFileSizeBytes)
            {
                return MarkdownFileReadResult.Invalid(
                    $"Markdown file must be {options.Value.MaxFileSizeBytes} bytes or smaller.");
            }

            await using var stream = OpenReadShared(canonicalPath);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync(cancellationToken);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
            return MarkdownFileReadResult.Valid(content, hash, info.Length);
        }
        catch (FileNotFoundException exception)
        {
            return MarkdownFileReadResult.Missing(exception.Message);
        }
        catch (DirectoryNotFoundException exception)
        {
            return MarkdownFileReadResult.Missing(exception.Message);
        }
        catch (Exception exception) when (exception is IOException
                                            or UnauthorizedAccessException
                                            or ArgumentException
                                            or NotSupportedException)
        {
            return MarkdownFileReadResult.Invalid(exception.Message);
        }
    }

    private static FileStream OpenReadShared(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
}
