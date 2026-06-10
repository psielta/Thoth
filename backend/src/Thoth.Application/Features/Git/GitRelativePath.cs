using Thoth.Application.Common.Exceptions;

namespace Thoth.Application.Features.Git;

public static class GitRelativePath
{
    public static string Normalize(string path)
    {
        var normalized = path.Trim().Replace('\\', '/');
        if (normalized.Length == 0 ||
            normalized.StartsWith("/", StringComparison.Ordinal) ||
            Path.IsPathRooted(normalized) ||
            HasParentTraversal(normalized))
        {
            throw new PathTraversalException("Only relative paths inside the working directory are allowed.");
        }

        return normalized;
    }

    private static bool HasParentTraversal(string path) =>
        path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment == "..");
}
