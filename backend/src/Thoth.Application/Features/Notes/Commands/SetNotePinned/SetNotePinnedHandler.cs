using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Commands.SetNotePinned;

public sealed class SetNotePinnedHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<SetNotePinnedCommand, NoteDto>
{
    public async Task<NoteDto> Handle(SetNotePinnedCommand request, CancellationToken cancellationToken)
    {
        var note = context.Notes
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (note is null)
        {
            throw new NotFoundException("Note was not found.");
        }

        note.IsPinned = request.IsPinned;
        await context.SaveChangesAsync(cancellationToken);

        return note.ToDto();
    }
}
