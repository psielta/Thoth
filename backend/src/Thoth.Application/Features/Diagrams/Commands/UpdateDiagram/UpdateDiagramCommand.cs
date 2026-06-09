using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Diagrams.Commands.UpdateDiagram;

public sealed record UpdateDiagramCommand(
    Guid Id,
    string Title,
    string Content,
    string? Description = null,
    string? MetadataJson = null) : IRequest<DiagramDto>;
