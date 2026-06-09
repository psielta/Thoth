using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notes.Queries.GetNote;

public sealed record GetNoteQuery(Guid Id) : IRequest<NoteDto>;
