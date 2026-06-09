using MediatR;

namespace Thoth.Application.Features.Notebooks.Commands.DeleteNotebook;

public sealed record DeleteNotebookCommand(Guid Id) : IRequest;
