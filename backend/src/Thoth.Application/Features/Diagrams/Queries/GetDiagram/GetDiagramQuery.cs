using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Diagrams.Queries.GetDiagram;

public sealed record GetDiagramQuery(Guid Id) : IRequest<DiagramDto>;
