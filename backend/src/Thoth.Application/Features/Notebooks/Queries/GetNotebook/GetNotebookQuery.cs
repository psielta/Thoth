using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notebooks.Queries.GetNotebook;

public sealed record GetNotebookQuery(Guid Id) : IRequest<NotebookDto>;
