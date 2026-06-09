using System.Globalization;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Features.Workflow;

internal static class TaskSummaryFactory
{
    public static TaskSummaryDto Build(IApplicationDbContext context, Prompt prompt, PromptWorkflow? workflow)
    {
        var workingDirectoryName = context.WorkingDirectories
            .Where(directory => directory.Id == prompt.WorkingDirectoryId)
            .Select(directory => directory.Name)
            .FirstOrDefault() ?? string.Empty;
        var hasChildPrompts = context.Prompts.Any(item => item.ParentPromptId == prompt.Id);
        // No maximo 1 plano por prompt. Mesma ordenacao deterministica do GetWorkflowBoardHandler
        // para o realtime bater com a query do quadro.
        var linkedDocument = context.LinkedDocuments
            .Where(document => document.PromptId == prompt.Id)
            .OrderBy(document => document.CreatedAtUtc)
            .ThenBy(document => document.Id)
            .Select(document => new { document.Id, document.PullRequestReference })
            .FirstOrDefault();
        var updatedAtUtc = workflow is not null && workflow.UpdatedAtUtc > prompt.UpdatedAtUtc
            ? workflow.UpdatedAtUtc
            : prompt.UpdatedAtUtc;
        IReadOnlyList<WorkflowPhaseDto> phases = workflow is null
            ? Array.Empty<WorkflowPhaseDto>()
            : context.PromptWorkflowPhases
                .Where(phase => phase.PromptWorkflowId == workflow.Id)
                .OrderBy(phase => phase.OrderIndex)
                .ToList()
                .Select(phase => phase.ToDto())
                .ToList();

        return new TaskSummaryDto(
            prompt.Id,
            prompt.WorkingDirectoryId,
            workingDirectoryName,
            prompt.TaskNumber,
            prompt.Title,
            prompt.Status,
            workflow?.Status,
            workflow?.CurrentPhaseId,
            workflow?.CurrentPhaseName,
            workflow?.CurrentPhaseColor,
            workflow?.CurrentActor,
            workflow?.EnteredCurrentPhaseAtUtc,
            workflow?.CurrentPhaseIteration ?? 1,
            workflow?.ReviewVerdictSourcePhaseName,
            updatedAtUtc,
            hasChildPrompts,
            linkedDocument is not null,
            linkedDocument?.Id,
            linkedDocument?.PullRequestReference,
            prompt.RowVersion.ToString(CultureInfo.InvariantCulture),
            phases,
            workflow is null ? null : workflow.RowVersion.ToString(CultureInfo.InvariantCulture));
    }
}
