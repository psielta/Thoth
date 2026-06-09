using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Commands.UpdateNote;

public sealed record UpdateNoteCommand(
    Guid Id,
    string Title,
    string ContentMarkdown) : IRequest<NoteDto>;
