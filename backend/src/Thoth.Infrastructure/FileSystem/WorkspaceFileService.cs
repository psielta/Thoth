using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Infrastructure.FileSystem;

public sealed class WorkspaceFileService(
    IMemoryCache cache,
    IDateTimeProvider dateTimeProvider,
    ILogger<WorkspaceFileService> logger) : IWorkspaceFileService
{
    private sealed record FileIndexEntry(string RelativePath, string FileName, bool IsDirectory);

    private static readonly string[] ContextFileNames = ["README.md", "CLAUDE.md", "AGENT.md"];
    private const long MaxContextFileBytes = 64 * 1024;
    private const long MaxEditorFileBytes = 1024 * 1024;
    private const int BinarySniffBytes = 8 * 1024;
    private const int MaxTotalContextChars = 48_000;

    private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules",
        ".git",
        ".hg",
        ".svn",
        "bin",
        "obj",
        ".vs",
        ".idea",
        "dist",
        "build",
        ".next",
        ".venv",
        "__pycache__",
        "target",
        ".gradle",
        "artifacts",
        "packages",
        "coverage",
        "TestResults",
        "log",
        "logs",
        ".cache",
        "tmp",
        "temp"
    };

    public Task<ValidatedPathResult> ValidatePathAsync(string absolutePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return Task.FromResult(ValidatedPathResult.Invalid("Path is required."));
        }

        try
        {
            if (!Path.IsPathRooted(absolutePath))
            {
                return Task.FromResult(ValidatedPathResult.Invalid("Path must be absolute."));
            }

            if (!Directory.Exists(absolutePath))
            {
                return Task.FromResult(ValidatedPathResult.Invalid("Path must point to an existing directory."));
            }

            var canonical = WorkspaceFilePath.CanonicalizeExistingPath(absolutePath);
            if (!Directory.Exists(canonical))
            {
                return Task.FromResult(ValidatedPathResult.Invalid("Path must point to a directory."));
            }

            return Task.FromResult(ValidatedPathResult.Valid(canonical));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Task.FromResult(ValidatedPathResult.Invalid(exception.Message));
        }
    }

    public async Task<string?> ReadWorkspaceContextAsync(string rootAbsolutePath, CancellationToken cancellationToken)
    {
        string rootCanonical;
        try
        {
            rootCanonical = WorkspaceFilePath.CanonicalizeExistingPath(rootAbsolutePath);
        }
        catch (Exception exception) when (exception is IOException
                                             or UnauthorizedAccessException
                                             or ArgumentException
                                             or NotSupportedException
                                             or PathTraversalException)
        {
            return null;
        }

        var sections = new List<string>();
        var totalChars = 0;

        foreach (var fileName in ContextFileNames)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string content;
            try
            {
                var candidateLogical = Path.GetFullPath(Path.Combine(rootCanonical, fileName));
                if (!File.Exists(candidateLogical))
                {
                    continue;
                }

                var candidateCanonical = WorkspaceFilePath.CanonicalizeExistingPath(candidateLogical);
                WorkspaceFilePath.EnsureContained(rootCanonical, candidateCanonical);

                var info = new FileInfo(candidateCanonical);
                if (info.Length == 0 || info.Length > MaxContextFileBytes)
                {
                    continue;
                }

                await using var stream = new FileStream(
                    candidateCanonical,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                content = (await reader.ReadToEndAsync(cancellationToken)).Trim();
            }
            catch (Exception exception) when (exception is IOException
                                                 or UnauthorizedAccessException
                                                 or ArgumentException
                                                 or NotSupportedException
                                                 or PathTraversalException)
            {
                continue;
            }

            if (content.Length == 0 || totalChars + content.Length > MaxTotalContextChars)
            {
                continue;
            }

            sections.Add($"### {fileName}\n\n{content}");
            totalChars += content.Length;
        }

        if (sections.Count == 0)
        {
            return null;
        }

        return "## Contexto do workspace\n\n"
             + "Os arquivos abaixo descrevem o projeto e suas convenções; use-os como contexto.\n\n"
             + string.Join("\n\n", sections);
    }

    public async Task<string?> ReadSelectedFilesAsync(
        string rootAbsolutePath,
        IReadOnlyList<string> relativePaths,
        CancellationToken cancellationToken)
    {
        string rootCanonical;
        try
        {
            rootCanonical = WorkspaceFilePath.CanonicalizeExistingPath(rootAbsolutePath);
        }
        catch (Exception exception) when (exception is IOException
                                             or UnauthorizedAccessException
                                             or ArgumentException
                                             or NotSupportedException
                                             or PathTraversalException)
        {
            return null;
        }

        var sections = new List<string>();
        var totalChars = 0;
        var seenCanonicalPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawPath in relativePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = rawPath?.Trim().TrimStart('@') ?? string.Empty;
            if (relativePath.Length == 0 || Path.IsPathRooted(relativePath) || WorkspaceFilePath.HasParentTraversal(relativePath))
            {
                continue;
            }

            string content;
            string normalizedDisplayPath;
            try
            {
                var candidateLogical = Path.GetFullPath(Path.Combine(rootCanonical, WorkspaceFilePath.NormalizeInputRelativePath(relativePath)));
                if (!File.Exists(candidateLogical))
                {
                    continue;
                }

                var candidateCanonical = WorkspaceFilePath.CanonicalizeExistingPath(candidateLogical);
                WorkspaceFilePath.EnsureContained(rootCanonical, candidateCanonical);
                if (!seenCanonicalPaths.Add(candidateCanonical))
                {
                    continue;
                }

                normalizedDisplayPath = WorkspaceFilePath.NormalizeRelativePath(Path.GetRelativePath(rootCanonical, candidateCanonical));

                var info = new FileInfo(candidateCanonical);
                if (info.Length == 0)
                {
                    continue;
                }

                if (info.Length > MaxContextFileBytes)
                {
                    logger.LogDebug(
                        "Selected context file {RelativePath} was skipped because it has {FileBytes} bytes and the per-file limit is {MaxContextFileBytes} bytes.",
                        normalizedDisplayPath,
                        info.Length,
                        MaxContextFileBytes);
                    continue;
                }

                await using var stream = new FileStream(
                    candidateCanonical,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                content = (await reader.ReadToEndAsync(cancellationToken)).Trim();
            }
            catch (Exception exception) when (exception is IOException
                                                 or UnauthorizedAccessException
                                                 or ArgumentException
                                                 or NotSupportedException
                                                 or PathTraversalException)
            {
                continue;
            }

            if (content.Length == 0 || totalChars + content.Length > MaxTotalContextChars)
            {
                if (content.Length > 0)
                {
                    logger.LogDebug(
                        "Selected context file {RelativePath} was skipped because it would exceed the total context limit of {MaxTotalContextChars} characters. Current characters: {CurrentTotalChars}; file characters: {FileChars}.",
                        normalizedDisplayPath,
                        MaxTotalContextChars,
                        totalChars,
                        content.Length);
                }

                continue;
            }

            sections.Add($"### {normalizedDisplayPath}\n\n{content}");
            totalChars += content.Length;
        }

        if (sections.Count == 0)
        {
            return null;
        }

        return "## Arquivos de contexto selecionados\n\n"
             + string.Join("\n\n", sections);
    }

    public async Task<IReadOnlyList<FileSearchResultDto>> SearchAsync(
        Guid workingDirectoryId,
        string rootAbsolutePath,
        string query,
        int limit,
        bool respectGitignore,
        CancellationToken cancellationToken)
    {
        RejectSuspiciousClientQuery(query);

        var rootCanonical = WorkspaceFilePath.CanonicalizeExistingPath(rootAbsolutePath);
        var boundedLimit = Math.Clamp(limit, 1, 200);
        var normalizedQuery = NormalizeSearchQuery(query);
        var cacheKey = $"file-index:{workingDirectoryId}:{rootCanonical}:{respectGitignore}";

        var index = await cache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return Task.FromResult(BuildIndex(rootCanonical, respectGitignore, cancellationToken));
        }) ?? Array.Empty<FileIndexEntry>();

        return SearchIndex(index, normalizedQuery, boundedLimit);
    }

    public Task<IReadOnlyList<FileReferenceValidationDto>> ValidateRelativePathsAsync(
        string rootAbsolutePath,
        IReadOnlyList<string> relativePaths,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rootCanonical = WorkspaceFilePath.CanonicalizeExistingPath(rootAbsolutePath);
        var results = relativePaths
            .Select(path => ValidateRelativePath(rootCanonical, path, cancellationToken))
            .ToList();

        return Task.FromResult<IReadOnlyList<FileReferenceValidationDto>>(results);
    }

    public Task<FileReferenceResolution> ResolveRelativePathAsync(
        string rootAbsolutePath,
        string relativePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || WorkspaceFilePath.HasParentTraversal(relativePath))
        {
            throw new PathTraversalException("Only relative paths inside the working directory are allowed.");
        }

        var rootCanonical = WorkspaceFilePath.CanonicalizeExistingPath(rootAbsolutePath);
        var candidateLogical = Path.GetFullPath(Path.Combine(rootCanonical, WorkspaceFilePath.NormalizeInputRelativePath(relativePath)));
        var exists = File.Exists(candidateLogical) || Directory.Exists(candidateLogical);
        var candidateCanonical = exists
            ? WorkspaceFilePath.CanonicalizeExistingPath(candidateLogical)
            : WorkspaceFilePath.TrimEndingDirectorySeparator(candidateLogical);
        WorkspaceFilePath.EnsureContained(rootCanonical, candidateCanonical);

        var normalized = WorkspaceFilePath.NormalizeRelativePath(Path.GetRelativePath(rootCanonical, candidateLogical));
        return Task.FromResult(new FileReferenceResolution(normalized, exists, dateTimeProvider.UtcNow));
    }

    public Task<IReadOnlyList<DirectoryEntryDto>> BrowseDirectoryAsync(
        string rootAbsolutePath,
        string relativeDirectoryPath,
        bool respectGitignore,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rootCanonical = WorkspaceFilePath.CanonicalizeExistingPath(rootAbsolutePath);
        var relativeDirectory = relativeDirectoryPath?.Trim().TrimStart('@') ?? string.Empty;
        if (relativeDirectory.Length > 0 &&
            (Path.IsPathRooted(relativeDirectory) || WorkspaceFilePath.HasParentTraversal(relativeDirectory)))
        {
            throw new PathTraversalException("Only relative paths inside the working directory are allowed.");
        }

        var directoryLogical = relativeDirectory.Length == 0
            ? rootCanonical
            : Path.GetFullPath(Path.Combine(rootCanonical, WorkspaceFilePath.NormalizeInputRelativePath(relativeDirectory)));

        if (!Directory.Exists(directoryLogical))
        {
            throw new FileNotFoundException("Directory was not found.", relativeDirectory);
        }

        var directoryCanonical = WorkspaceFilePath.CanonicalizeExistingPath(directoryLogical);
        WorkspaceFilePath.EnsureContained(rootCanonical, directoryCanonical);

        var matcher = respectGitignore ? BuildGitignoreMatcher(rootCanonical) : null;
        var entries = new List<DirectoryEntryDto>();

        IEnumerable<string> fileSystemEntries;
        try
        {
            fileSystemEntries = Directory.EnumerateFileSystemEntries(directoryCanonical);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new FileNotFoundException("Directory was not found.", relativeDirectory);
        }

        foreach (var entry in fileSystemEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FileAttributes attributes;
            try
            {
                attributes = File.GetAttributes(entry);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if ((attributes & FileAttributes.System) != 0 || (attributes & FileAttributes.ReparsePoint) != 0)
            {
                continue;
            }

            var isDirectory = (attributes & FileAttributes.Directory) != 0;
            var name = Path.GetFileName(entry);
            if (isDirectory && IgnoredDirectoryNames.Contains(name))
            {
                continue;
            }

            var candidateCanonical = WorkspaceFilePath.TrimEndingDirectorySeparator(Path.GetFullPath(entry));
            WorkspaceFilePath.EnsureContained(rootCanonical, candidateCanonical);
            var relativePath = WorkspaceFilePath.NormalizeRelativePath(Path.GetRelativePath(rootCanonical, entry));

            if (matcher is not null && !matcher.Match(relativePath).HasMatches)
            {
                continue;
            }

            entries.Add(new DirectoryEntryDto(relativePath, name, isDirectory));
        }

        return Task.FromResult<IReadOnlyList<DirectoryEntryDto>>(
            entries
                .OrderByDescending(entry => entry.IsDirectory)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    public async Task<FileContentDto> ReadFileAsync(
        string rootAbsolutePath,
        string relativePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedInput = relativePath?.Trim().TrimStart('@') ?? string.Empty;
        if (normalizedInput.Length == 0 ||
            Path.IsPathRooted(normalizedInput) ||
            WorkspaceFilePath.HasParentTraversal(normalizedInput))
        {
            throw new PathTraversalException("Only relative paths inside the working directory are allowed.");
        }

        var rootCanonical = WorkspaceFilePath.CanonicalizeExistingPath(rootAbsolutePath);
        var candidateLogical = Path.GetFullPath(Path.Combine(rootCanonical, WorkspaceFilePath.NormalizeInputRelativePath(normalizedInput)));

        if (!File.Exists(candidateLogical) || Directory.Exists(candidateLogical))
        {
            throw new FileNotFoundException("File was not found.", normalizedInput);
        }

        var candidateCanonical = WorkspaceFilePath.CanonicalizeExistingPath(candidateLogical);
        WorkspaceFilePath.EnsureContained(rootCanonical, candidateCanonical);

        var normalizedRelativePath = WorkspaceFilePath.NormalizeRelativePath(Path.GetRelativePath(rootCanonical, candidateCanonical));
        var info = new FileInfo(candidateCanonical);
        var truncated = info.Length > MaxEditorFileBytes;
        var bytesToRead = (int)Math.Min(info.Length, MaxEditorFileBytes);

        await using var stream = new FileStream(
            candidateCanonical,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        var sniffLength = (int)Math.Min(info.Length, BinarySniffBytes);
        var sniffBuffer = new byte[sniffLength];
        var readSniff = await stream.ReadAsync(sniffBuffer.AsMemory(0, sniffLength), cancellationToken);
        var isBinary = sniffBuffer.AsSpan(0, readSniff).Contains((byte)0);

        if (isBinary)
        {
            return new FileContentDto(normalizedRelativePath, string.Empty, info.Length, truncated, true);
        }

        var contentBuffer = new byte[bytesToRead];
        if (readSniff > 0)
        {
            sniffBuffer.AsSpan(0, readSniff).CopyTo(contentBuffer);
        }

        var totalRead = readSniff;
        while (totalRead < bytesToRead)
        {
            var read = await stream.ReadAsync(contentBuffer.AsMemory(totalRead, bytesToRead - totalRead), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        var content = Encoding.UTF8.GetString(contentBuffer, 0, totalRead);
        return new FileContentDto(normalizedRelativePath, content, info.Length, truncated, false);
    }

    private static FileReferenceValidationDto ValidateRelativePath(
        string rootCanonical,
        string rawPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var relativePath = rawPath.Trim().TrimStart('@');
        if (relativePath.Length == 0)
        {
            return new FileReferenceValidationDto(rawPath, string.Empty, false, false, "Path is required.");
        }

        try
        {
            if (Path.IsPathRooted(relativePath) || WorkspaceFilePath.HasParentTraversal(relativePath))
            {
                return new FileReferenceValidationDto(
                    rawPath,
                    relativePath,
                    false,
                    false,
                    "Only relative paths inside the working directory are allowed.");
            }

            var candidateLogical = Path.GetFullPath(Path.Combine(rootCanonical, WorkspaceFilePath.NormalizeInputRelativePath(relativePath)));
            var exists = File.Exists(candidateLogical) || Directory.Exists(candidateLogical);
            var candidateCanonical = exists
                ? WorkspaceFilePath.CanonicalizeExistingPath(candidateLogical)
                : WorkspaceFilePath.TrimEndingDirectorySeparator(candidateLogical);

            WorkspaceFilePath.EnsureContained(rootCanonical, candidateCanonical);

            if (!exists)
            {
                return new FileReferenceValidationDto(
                    rawPath,
                    WorkspaceFilePath.NormalizeRelativePath(Path.GetRelativePath(rootCanonical, candidateLogical)),
                    false,
                    false,
                    "File or directory was not found.");
            }

            var isDirectory = Directory.Exists(candidateCanonical);
            return new FileReferenceValidationDto(
                rawPath,
                WorkspaceFilePath.NormalizeRelativePath(Path.GetRelativePath(rootCanonical, candidateCanonical)),
                true,
                isDirectory,
                null);
        }
        catch (Exception exception) when (exception is IOException
                                            or UnauthorizedAccessException
                                            or ArgumentException
                                            or NotSupportedException
                                            or PathTraversalException)
        {
            return new FileReferenceValidationDto(rawPath, relativePath, false, false, exception.Message);
        }
    }

    private static IReadOnlyList<FileIndexEntry> BuildIndex(
        string rootCanonical,
        bool respectGitignore,
        CancellationToken cancellationToken)
    {
        var matcher = respectGitignore ? BuildGitignoreMatcher(rootCanonical) : null;
        var indexEntries = new List<FileIndexEntry>();
        var directories = new Queue<string>();
        directories.Enqueue(rootCanonical);

        while (directories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDirectory = directories.Dequeue();

            IEnumerable<string> fileSystemEntries;
            try
            {
                fileSystemEntries = Directory.EnumerateFileSystemEntries(currentDirectory);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var entry in fileSystemEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(entry);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                if ((attributes & FileAttributes.System) != 0 || (attributes & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                var isDirectory = (attributes & FileAttributes.Directory) != 0;
                var name = Path.GetFileName(entry);
                if (isDirectory && IgnoredDirectoryNames.Contains(name))
                {
                    continue;
                }

                var candidateCanonical = WorkspaceFilePath.TrimEndingDirectorySeparator(Path.GetFullPath(entry));
                WorkspaceFilePath.EnsureContained(rootCanonical, candidateCanonical);
                var relativePath = WorkspaceFilePath.NormalizeRelativePath(Path.GetRelativePath(rootCanonical, entry));

                if (matcher is not null && !matcher.Match(relativePath).HasMatches)
                {
                    continue;
                }

                indexEntries.Add(new FileIndexEntry(relativePath, name, isDirectory));

                if (isDirectory)
                {
                    directories.Enqueue(entry);
                }
            }
        }

        return indexEntries;
    }

    private static IReadOnlyList<FileSearchResultDto> SearchIndex(
        IReadOnlyList<FileIndexEntry> index,
        string query,
        int limit)
    {
        return index
            .Select(entry => new
            {
                Entry = entry,
                Score = CalculateScore(entry.RelativePath, entry.FileName, query)
            })
            .Where(result => result.Score >= 0)
            .Select(result => new FileSearchResultDto(
                result.Entry.RelativePath,
                result.Entry.FileName,
                result.Entry.IsDirectory,
                result.Score))
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.RelativePath.Count(character => character == '/'))
            .ThenBy(result => result.RelativePath.Length)
            .ThenBy(result => result.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static int CalculateScore(string relativePath, string fileName, string query)
    {
        if (query.Length == 0)
        {
            return 1_000 - relativePath.Count(character => character == '/');
        }

        var normalizedFileName = fileName.ToUpperInvariant();
        var normalizedRelativePath = relativePath.ToUpperInvariant();
        var normalizedQuery = query.ToUpperInvariant();
        if (normalizedFileName == normalizedQuery)
        {
            return 10_000;
        }

        if (normalizedRelativePath == normalizedQuery)
        {
            return 9_500;
        }

        var exactFileNameIndex = normalizedFileName.IndexOf(normalizedQuery, StringComparison.Ordinal);
        if (exactFileNameIndex >= 0)
        {
            return 8_000 - exactFileNameIndex;
        }

        var exactPathIndex = normalizedRelativePath.IndexOf(normalizedQuery, StringComparison.Ordinal);
        if (exactPathIndex >= 0)
        {
            return 7_000 - exactPathIndex;
        }

        var pathScore = FuzzyScore(relativePath, query);
        var fileScore = FuzzyScore(fileName, query);
        if (pathScore < 0 && fileScore < 0)
        {
            return -1;
        }

        return Math.Max(pathScore, 0) + Math.Max(fileScore, 0) + (fileScore > 0 ? 100 : 0);
    }

    private static int FuzzyScore(string value, string query)
    {
        var valueIndex = 0;
        var score = 0;
        var previousMatchIndex = -2;

        foreach (var queryCharacter in query)
        {
            var found = false;
            while (valueIndex < value.Length)
            {
                if (char.ToUpperInvariant(value[valueIndex]) == char.ToUpperInvariant(queryCharacter))
                {
                    score += 10;
                    if (valueIndex == 0 || value[valueIndex - 1] is '/' or '\\' or '-' or '_' or '.')
                    {
                        score += 8;
                    }

                    if (valueIndex == previousMatchIndex + 1)
                    {
                        score += 5;
                    }

                    previousMatchIndex = valueIndex;
                    valueIndex++;
                    found = true;
                    break;
                }

                valueIndex++;
            }

            if (!found)
            {
                return -1;
            }
        }

        return score;
    }

    private static Matcher? BuildGitignoreMatcher(string rootCanonical)
    {
        var gitignorePath = Path.Combine(rootCanonical, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            return null;
        }

        var patterns = File.ReadAllLines(gitignorePath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToList();

        if (patterns.Count == 0)
        {
            return null;
        }

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude("**");
        matcher.AddExcludePatterns(patterns);
        return matcher;
    }

    private static void RejectSuspiciousClientQuery(string query)
    {
        var normalizedQuery = NormalizeSearchQuery(query);
        if (Path.IsPathRooted(normalizedQuery) || WorkspaceFilePath.HasParentTraversal(normalizedQuery))
        {
            throw new PathTraversalException("Search query cannot be an absolute or parent-relative path.");
        }
    }

    private static string NormalizeSearchQuery(string query) =>
        query.Trim().TrimStart('@');
}
