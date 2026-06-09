using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Domain.Notebooks;

namespace Thoth.Application.Features.Notes.Commands.CreateNote;

public sealed class CreateNoteHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<CreateNoteCommand, NoteDto>
{
    public async Task<NoteDto> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
        var notebookOwned = context.Notebooks
            .Any(notebook => notebook.Id == request.NotebookId && notebook.OwnerId == currentUser.UserId);

        if (!notebookOwned)
        {
            throw new NotFoundException("Notebook was not found.");
        }

        var note = new Note
        {
            NotebookId = request.NotebookId,
            Title = request.Title.Trim(),
            ContentMarkdown = request.ContentMarkdown ?? string.Empty,
            OwnerId = currentUser.UserId
        };

        context.Add(note);
        await context.SaveChangesAsync(cancellationToken);

        return note.ToDto();
    }
}
