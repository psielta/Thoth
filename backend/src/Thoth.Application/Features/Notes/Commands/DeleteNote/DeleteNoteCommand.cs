using MediatR;

namespace Thoth.Application.Features.Notes.Commands.DeleteNote;

public sealed record DeleteNoteCommand(Guid Id) : IRequest;
