using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Queries.GetNote;

public sealed class GetNoteHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetNoteQuery, NoteDto>
{
    public Task<NoteDto> Handle(GetNoteQuery request, CancellationToken cancellationToken)
    {
        var note = context.Notes
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (note is null)
        {
            throw new NotFoundException("Note was not found.");
        }

        return Task.FromResult(note.ToDto());
    }
}
