namespace Thoth.Application.Common.Models;

public sealed record DirectoryEntryDto(string RelativePath, string Name, bool IsDirectory);