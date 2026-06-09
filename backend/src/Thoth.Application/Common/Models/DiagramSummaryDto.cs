using Thoth.Domain.Diagrams;

namespace Thoth.Application.Common.Models;

public sealed record DiagramSummaryDto(
    Guid Id,
    Guid WorkingDirectoryId,
    string WorkingDirectoryName,
    string Title,
    string? Description,
    DiagramType Type,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
