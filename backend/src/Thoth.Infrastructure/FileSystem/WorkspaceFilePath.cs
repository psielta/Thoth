using Thoth.Application.Common.Exceptions;

namespace Thoth.Infrastructure.FileSystem;

public static class WorkspaceFilePath
{
    public static string CanonicalizeExistingPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath)
            ?? throw new PathTraversalException("Path root could not be resolved.");
        var current = root;
        var remaining = fullPath[root.Length..]
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in remaining)
        {
            current = Path.Combine(current, segment);

            if (!File.Exists(current) && !Directory.Exists(current))
            {
                continue;
            }

            FileSystemInfo info = Directory.Exists(current)
                ? new DirectoryInfo(current)
                : new FileInfo(current);

            var target = info.ResolveLinkTarget(returnFinalTarget: true);
            if (target is not null)
            {
                current = Path.GetFullPath(target.FullName);
            }
        }

        return TrimEndingDirectorySeparator(Path.GetFullPath(current));
    }

    public static void EnsureContained(string rootCanonical, string candidateCanonical)
    {
        var relative = Path.GetRelativePath(rootCanonical, candidateCanonical);
        if (relative == ".")
        {
            return;
        }

        if (relative == ".." ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
        {
            throw new PathTraversalException("Path escapes the working directory.");
        }
    }

    public static bool HasParentTraversal(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment == "..");

    public static string NormalizeRelativePath(string relativePath) =>
        relativePath.Replace('\\', '/');

    public static string NormalizeInputRelativePath(string relativePath) =>
        relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

    public static string TrimEndingDirectorySeparator(string path) =>
        Path.TrimEndingDirectorySeparator(path);

    public static string CreateFileKey(string relativePath)
    {
        var normalized = relativePath.Trim().TrimStart('@').Replace('\\', '/');
        if (normalized.Length == 0 ||
            Path.IsPathRooted(normalized) ||
            HasParentTraversal(normalized))
        {
            throw new PathTraversalException("Only relative paths inside the working directory are allowed.");
        }

        return normalized.ToLowerInvariant();
    }
}