using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Notebooks.Commands.SetNotebookArchived;

public sealed class SetNotebookArchivedHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<SetNotebookArchivedCommand, NotebookDto>
{
    public async Task<NotebookDto> Handle(SetNotebookArchivedCommand request, CancellationToken cancellationToken)
    {
        var notebook = context.Notebooks
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (notebook is null)
        {
            throw new NotFoundException("Notebook was not found.");
        }

        notebook.IsArchived = request.IsArchived;
        await context.SaveChangesAsync(cancellationToken);

        return notebook.ToDtoWithDetails(context);
    }
}
