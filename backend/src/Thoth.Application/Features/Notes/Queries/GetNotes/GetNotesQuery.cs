using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Queries.GetNotes;

public sealed record GetNotesQuery(
    Guid? NotebookId = null,
    string? Search = null,
    bool IncludeArchived = false) : IRequest<IReadOnlyList<NoteDto>>;
