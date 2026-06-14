namespace Thoth.Application.Common.Models;

public sealed record TerminalOutputHistoryDto(
    Guid SessionId,
    long StartOffset,
    long EndOffset,
    string DataBase64,
    bool IsTruncated);
