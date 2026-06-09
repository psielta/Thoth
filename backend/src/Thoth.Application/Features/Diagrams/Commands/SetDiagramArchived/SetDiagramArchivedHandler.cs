using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Diagrams.Commands.SetDiagramArchived;

public sealed class SetDiagramArchivedHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<SetDiagramArchivedCommand, DiagramDto>
{
    public async Task<DiagramDto> Handle(SetDiagramArchivedCommand request, CancellationToken cancellationToken)
    {
        var diagram = context.Diagrams
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (diagram is null)
        {
            throw new NotFoundException("Diagram was not found.");
        }

        diagram.IsArchived = request.IsArchived;
        await context.SaveChangesAsync(cancellationToken);

        return diagram.ToDto();
    }
}
