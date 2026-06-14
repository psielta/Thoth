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

        // Group the in-memory sessions by their owning prompt; terminals within a prompt are ordered by creation.
        var sessionsByPromptId = sessions
            .Where(session => session.PromptId.HasValue)
            .GroupBy(session => session.PromptId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(session => session.CreatedAtUtc).ToList());

        var terminalPromptIds = sessionsByPromptId.Keys.ToList();

        // Hierarquia + posse de cada prompt que tem terminal. Terminais de filhos sao exibidos sob o pai,
        // entao precisamos do ParentPromptId (para a chave de grupo) e do Title (para o badge do filho).
        var promptInfos = (
            from prompt in context.Prompts
            where prompt.OwnerId == currentUser.UserId && terminalPromptIds.Contains(prompt.Id)
            select new { prompt.Id, prompt.ParentPromptId, prompt.Title })
            .ToDictionary(info => info.Id);

        // Chave do grupo = pai (se houver), senao o proprio prompt. Salto unico (fluxo e de um nivel).
        var groupIds = promptInfos.Values
            .Select(info => info.ParentPromptId ?? info.Id)
            .Distinct()
            .ToList();

        // Metadados do grupo (titulo/workspace/arquivado) vem do prompt do grupo (o pai). O workspace join
        // expoe o nome do diretorio para exibicao.
        var groupMeta = (
            from prompt in context.Prompts
            join workingDirectory in context.WorkingDirectories
                on prompt.WorkingDirectoryId equals workingDirectory.Id
            where prompt.OwnerId == currentUser.UserId && groupIds.Contains(prompt.Id)
            select new
            {
                prompt.Id,
                prompt.Title,
                prompt.WorkingDirectoryId,
                WorkingDirectoryName = workingDirectory.Name,
                IsArchived = prompt.Status == PromptStatus.Archived
            })
            .ToDictionary(meta => meta.Id);

        // Distribui cada sessao no grupo do seu pai (ou no proprio), marcando terminais de filhos.
        var terminalsByGroup = new Dictionary<Guid, List<TerminalSessionDescriptor>>();
        foreach (var (promptId, descriptors) in sessionsByPromptId)
        {
            if (!promptInfos.TryGetValue(promptId, out var info))
            {
                continue; // prompt sumiu ou nao pertence ao usuario atual
            }

            var groupId = info.ParentPromptId ?? promptId;
            if (!groupMeta.ContainsKey(groupId))
            {
                continue; // grupo (pai) sumiu ou nao pertence ao usuario atual
            }

            var isChild = groupId != promptId;
            if (!terminalsByGroup.TryGetValue(groupId, out var bucket))
            {
                bucket = [];
                terminalsByGroup[groupId] = bucket;
            }

            foreach (var descriptor in descriptors)
            {
                bucket.Add(isChild
                    ? descriptor with { IsChild = true, OwnerPromptTitle = info.Title }
                    : descriptor);
            }
        }

        IReadOnlyList<PromptTerminalsGroupDto> result = terminalsByGroup
            .Select(entry =>
            {
                var meta = groupMeta[entry.Key];
                IReadOnlyList<TerminalSessionDescriptor> terminals = entry.Value
                    .OrderBy(terminal => terminal.IsChild)
                    .ThenBy(terminal => terminal.CreatedAtUtc)
                    .ToList();
                return new PromptTerminalsGroupDto(
                    meta.Id,
                    meta.Title,
                    meta.WorkingDirectoryId,
                    meta.WorkingDirectoryName,
                    meta.IsArchived,
                    terminals);
            })
            .OrderByDescending(group => group.Terminals.Max(terminal => terminal.CreatedAtUtc))
            .ToList();

        return Task.FromResult(result);
    }
}
