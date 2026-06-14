using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Prompts;

namespace Thoth.Application.Features.Terminals.Queries.ListTerminalSessions;

public sealed class ListTerminalSessionsHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    ITerminalSessionCoordinator terminalCoordinator)
    : IRequestHandler<ListTerminalSessionsQuery, IReadOnlyList<TerminalSessionDescriptor>>
{
    public Task<IReadOnlyList<TerminalSessionDescriptor>> Handle(
        ListTerminalSessionsQuery request,
        CancellationToken cancellationToken)
    {
        _ = PromptMutationHelpers.GetPrompt(context, request.PromptId, currentUser.UserId);

        // Terminais proprios do prompt (ja ordenados por CreatedAtUtc pelo coordinator).
        var own = terminalCoordinator.ListForPrompt(request.PromptId);

        // Terminais dos prompts filhos sao "promovidos" para a visao do pai, marcados como filho.
        var children = context.Prompts
            .Where(prompt => prompt.ParentPromptId == request.PromptId && prompt.OwnerId == currentUser.UserId)
            .Select(prompt => new { prompt.Id, prompt.Title })
            .ToList();

        var childTerminals = children
            .SelectMany(child => terminalCoordinator.ListForPrompt(child.Id)
                .Select(descriptor => descriptor with { IsChild = true, OwnerPromptTitle = child.Title }))
            .OrderBy(descriptor => descriptor.CreatedAtUtc)
            .ToList();

        // Proprios primeiro, depois os filhos: mantem estavel a numeracao "Terminal N" das abas do pai.
        IReadOnlyList<TerminalSessionDescriptor> result = own.Concat(childTerminals).ToList();
        return Task.FromResult(result);
    }
}