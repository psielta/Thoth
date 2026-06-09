using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Diagrams;

namespace Thoth.Application.Features.Diagrams.Commands.CreateDiagram;

public sealed record CreateDiagramCommand(
    Guid WorkingDirectoryId,
    string Title,
    DiagramType Type,
    string? Description = null,
    string? Content = null,
    string? MetadataJson = null) : IRequest<DiagramDto>;
