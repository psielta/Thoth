using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notebooks.Queries.GetNotebook;

public sealed class GetNotebookHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetNotebookQuery, NotebookDto>
{
    public Task<NotebookDto> Handle(GetNotebookQuery request, CancellationToken cancellationToken)
    {
        var notebook = context.Notebooks
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (notebook is null)
        {
            throw new NotFoundException("Notebook was not found.");
        }

        return Task.FromResult(notebook.ToDtoWithDetails(context));
    }
}
