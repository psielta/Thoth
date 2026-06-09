using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Commands.CreateNote;

public sealed record CreateNoteCommand(
    Guid NotebookId,
    string Title,
    string? ContentMarkdown = null) : IRequest<NoteDto>;
