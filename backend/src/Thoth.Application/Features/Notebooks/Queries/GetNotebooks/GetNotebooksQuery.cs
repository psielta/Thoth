using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notebooks.Queries.GetNotebooks;

public sealed record GetNotebooksQuery(bool IncludeArchived = false) : IRequest<IReadOnlyList<NotebookDto>>;
