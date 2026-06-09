namespace Thoth.Application.Common.Models;

public sealed record ValidatePathResponse(bool IsValid, string? CanonicalPath, string? Error);
