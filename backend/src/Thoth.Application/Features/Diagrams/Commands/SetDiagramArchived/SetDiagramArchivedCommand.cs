using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Diagrams.Commands.SetDiagramArchived;

public sealed record SetDiagramArchivedCommand(Guid Id, bool IsArchived) : IRequest<DiagramDto>;
