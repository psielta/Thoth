namespace Thoth.Infrastructure.FileSystem;

internal static class LinkedDocumentPath
{
    public static string CanonicalizeExistingPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath)
            ?? throw new IOException("Path root could not be resolved.");
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

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(current));
    }

    public static string CreateKey(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path))
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .ToLowerInvariant();
}
