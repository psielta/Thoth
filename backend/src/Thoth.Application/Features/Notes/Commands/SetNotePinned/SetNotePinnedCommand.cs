using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Commands.SetNotePinned;

public sealed record SetNotePinnedCommand(Guid Id, bool IsPinned) : IRequest<NoteDto>;
