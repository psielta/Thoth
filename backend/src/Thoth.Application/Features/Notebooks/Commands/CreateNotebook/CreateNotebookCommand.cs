using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notebooks.Commands.CreateNotebook;

public sealed record CreateNotebookCommand(
    string Title,
    string? Description = null,
    Guid? WorkingDirectoryId = null) : IRequest<NotebookDto>;
