using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Commands.SetNoteArchived;

public sealed record SetNoteArchivedCommand(Guid Id, bool IsArchived) : IRequest<NoteDto>;
