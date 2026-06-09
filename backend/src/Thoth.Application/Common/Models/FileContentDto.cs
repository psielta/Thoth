namespace Thoth.Application.Common.Models;

public sealed record FileContentDto(
    string RelativePath,
    string Content,
    long SizeBytes,
    bool Truncated,
    bool IsBinary);