using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Git;

public static class GitPorcelainParser
{
    public static IReadOnlyList<GitFileStatusDto> Parse(string output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return Array.Empty<GitFileStatusDto>();
        }

        var entries = new List<GitFileStatusDto>();
        var tokens = output.Split('\0', StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            if (token.Length < 4 || token[2] != ' ')
            {
                continue;
            }

            var x = token[0];
            var y = token[1];
            if (x == '!' && y == '!')
            {
                continue;
            }

            var path = NormalizePath(token[3..]);
            if (path.Length == 0)
            {
                continue;
            }

            var status = MapStatus(x, y);
            string? originalPath = null;
            if (IsRenameOrCopy(x, y))
            {
                if (index + 1 >= tokens.Length)
                {
                    continue;
                }

                originalPath = NormalizePath(tokens[++index]);
                if (originalPath.Length == 0)
                {
                    originalPath = null;
                }
            }

            entries.Add(new GitFileStatusDto(path, status, originalPath));
        }

        return entries;
    }

    private static GitFileChangeStatus MapStatus(char x, char y)
    {
        if (x == '?' && y == '?')
        {
            return GitFileChangeStatus.Untracked;
        }

        if (x == 'U' || y == 'U' || (x == 'A' && y == 'A') || (x == 'D' && y == 'D'))
        {
            return GitFileChangeStatus.Modified;
        }

        if (IsRenameOrCopy(x, y))
        {
            return GitFileChangeStatus.Renamed;
        }

        if (x == 'A' || y == 'A')
        {
            return GitFileChangeStatus.Added;
        }

        if (x == 'D' || y == 'D')
        {
            return GitFileChangeStatus.Deleted;
        }

        return GitFileChangeStatus.Modified;
    }

    private static bool IsRenameOrCopy(char x, char y) =>
        x is 'R' or 'C' || y is 'R' or 'C';

    private static string NormalizePath(string path) =>
        path.Trim().Replace('\\', '/');
}
