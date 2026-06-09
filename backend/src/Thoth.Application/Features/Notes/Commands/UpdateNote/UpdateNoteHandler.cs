using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Commands.UpdateNote;

public sealed class UpdateNoteHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<UpdateNoteCommand, NoteDto>
{
    public async Task<NoteDto> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
        var note = context.Notes
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (note is null)
        {
            throw new NotFoundException("Note was not found.");
        }

        note.Title = request.Title.Trim();
        note.ContentMarkdown = request.ContentMarkdown ?? string.Empty;

        await context.SaveChangesAsync(cancellationToken);

        return note.ToDto();
    }
}
