using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.Terminals.Queries.ListAllTerminalSessions;

public sealed class ListAllTerminalSessionsHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    ITerminalSessionCoordinator terminalCoordinator)
    : IRequestHandler<ListAllTerminalSessionsQuery, IReadOnlyList<PromptTerminalsGroupDto>>
{
    public Task<IReadOnlyList<PromptTerminalsGroupDto>> Handle(
        ListAllTerminalSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var sessions = terminalCoordinator.ListAll();
        if (sessions.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<PromptTerminalsGroupDto>>(Array.Empty<PromptTerminalsGroupDto>());
        }

        // Group the in-memory sessions by prompt; terminals within a prompt are ordered by creation.
        var sessionsByPrompt = sessions
            .GroupBy(session => session.PromptId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<TerminalSessionDescriptor>)group
                    .OrderBy(session => session.CreatedAtUtc)
                    .ToList());

        var promptIds = sessionsByPrompt.Keys.ToList();

        // Ownership gate: the terminal registry is global across users, so only prompts owned by
        // the current user survive. The workspace join exposes the directory name for display.
        var ownedPrompts = (
            from prompt in context.Prompts
            join workingDirectory in context.WorkingDirectories
                on prompt.WorkingDirectoryId equals workingDirectory.Id
            where prompt.OwnerId == currentUser.UserId && promptIds.Contains(prompt.Id)
            select new
            {
                prompt.Id,
                prompt.Title,
                prompt.WorkingDirectoryId,
                WorkingDirectoryName = workingDirectory.Name,
                IsArchived = prompt.Status == PromptStatus.Archived
            })
            .ToList();

        IReadOnlyList<PromptTerminalsGroupDto> result = ownedPrompts
            .Select(prompt => new PromptTerminalsGroupDto(
                prompt.Id,
                prompt.Title,
                prompt.WorkingDirectoryId,
                prompt.WorkingDirectoryName,
                prompt.IsArchived,
                sessionsByPrompt[prompt.Id]))
            .OrderByDescending(group => group.Terminals.Max(terminal => terminal.CreatedAtUtc))
            .ToList();

        return Task.FromResult(result);
    }
}
