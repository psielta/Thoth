using MediatR;
using Microsoft.EntityFrameworkCore;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;

namespace Thoth.Application.Features.Prompts.Commands.ReorderBoardColumn;

public sealed class ReorderBoardColumnHandler(
    IApplicationDbContext context,
    IPromptNotifier promptNotifier,
    ICurrentUser currentUser)
    : IRequestHandler<ReorderBoardColumnCommand>
{
    public async Task Handle(ReorderBoardColumnCommand request, CancellationToken cancellationToken)
    {
        var orderedIds = request.OrderedPromptIds;
        var ownedCount = context.Prompts
            .Count(prompt => orderedIds.Contains(prompt.Id) && prompt.OwnerId == currentUser.UserId);

        if (ownedCount != orderedIds.Count)
        {
            throw new NotFoundException("One or more prompts were not found.");
        }

        for (var index = 0; index < orderedIds.Count; index++)
        {
            var promptId = orderedIds[index];
            var rank = index + 1d;
            await context.Prompts
                .Where(prompt => prompt.Id == promptId && prompt.OwnerId == currentUser.UserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(prompt => prompt.BoardRank, rank),
                    cancellationToken);
        }

        await promptNotifier.BoardReorderedAsync(cancellationToken);
    }
}
