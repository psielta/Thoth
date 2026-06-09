using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Diagrams;

namespace Thoth.Application.Features.Diagrams.Queries.GetDiagrams;

public sealed record GetDiagramsQuery(
    Guid? WorkingDirectoryId = null,
    string? Search = null,
    DiagramType? Type = null,
    bool IncludeArchived = false) : IRequest<IReadOnlyList<DiagramSummaryDto>>;
