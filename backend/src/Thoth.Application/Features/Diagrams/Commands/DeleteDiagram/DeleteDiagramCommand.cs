using MediatR;

namespace Thoth.Application.Features.Diagrams.Commands.DeleteDiagram;

public sealed record DeleteDiagramCommand(Guid Id) : IRequest;
