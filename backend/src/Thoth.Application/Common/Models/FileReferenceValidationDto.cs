namespace Thoth.Application.Common.Models;

public sealed record FileReferenceValidationDto(
    string RawPath,
    string RelativePath,
    bool Exists,
    bool IsDirectory,
    string? Error);
