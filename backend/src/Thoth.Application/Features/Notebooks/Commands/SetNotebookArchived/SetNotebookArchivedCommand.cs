using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notebooks.Commands.SetNotebookArchived;

public sealed record SetNotebookArchivedCommand(Guid Id, bool IsArchived) : IRequest<NotebookDto>;
