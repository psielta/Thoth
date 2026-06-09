using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Queries.GetNotes;

public sealed class GetNotesHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetNotesQuery, IReadOnlyList<NoteDto>>
{
    public Task<IReadOnlyList<NoteDto>> Handle(GetNotesQuery request, CancellationToken cancellationToken)
    {
        var notes = context.Notes.Where(note => note.OwnerId == currentUser.UserId);

        if (request.NotebookId is { } notebookId)
        {
            notes = notes.Where(note => note.NotebookId == notebookId);
        }

        if (!request.IncludeArchived)
        {
            notes = notes.Where(note => !note.IsArchived);
        }

        var term = request.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(term))
        {
            notes = notes.Where(note =>
                note.Title.ToLower().Contains(term) || note.ContentMarkdown.ToLower().Contains(term));
        }

        IReadOnlyList<NoteDto> result = notes
            .OrderByDescending(note => note.IsPinned)
            .ThenByDescending(note => note.UpdatedAtUtc)
            .Select(note => note.ToDto())
            .ToList();

        return Task.FromResult(result);
    }
}
