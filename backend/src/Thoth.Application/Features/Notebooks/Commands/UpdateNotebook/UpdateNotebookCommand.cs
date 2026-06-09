using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notebooks.Commands.UpdateNotebook;

public sealed record UpdateNotebookCommand(
    Guid Id,
    string Title,
    string? Description = null,
    Guid? WorkingDirectoryId = null) : IRequest<NotebookDto>;
