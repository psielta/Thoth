using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Commands.SetNoteArchived;

public sealed class SetNoteArchivedHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<SetNoteArchivedCommand, NoteDto>
{
    public async Task<NoteDto> Handle(SetNoteArchivedCommand request, CancellationToken cancellationToken)
    {
        var note = context.Notes
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (note is null)
        {
            throw new NotFoundException("Note was not found.");
        }

        note.IsArchived = request.IsArchived;
        await context.SaveChangesAsync(cancellationToken);

        return note.ToDto();
    }
}
