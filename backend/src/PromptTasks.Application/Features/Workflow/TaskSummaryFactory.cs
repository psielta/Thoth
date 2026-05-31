using System.Globalization;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;
using PromptTasks.Domain.Workflows;

namespace PromptTasks.Application.Features.Workflow;

internal static class TaskSummaryFactory
{
    public static TaskSummaryDto Build(IApplicationDbContext context, Prompt prompt, PromptWorkflow? workflow)
    {
        var workingDirectoryName = context.WorkingDirectories
            .Where(directory => directory.Id == prompt.WorkingDirectoryId)
            .Select(directory => directory.Name)
            .FirstOrDefault() ?? string.Empty;
        var hasChildPrompts = context.Prompts.Any(item => item.ParentPromptId == prompt.Id);
        var hasLinkedPlan = context.LinkedDocuments.Any(document => document.PromptId == prompt.Id);
        var updatedAtUtc = workflow is not null && workflow.UpdatedAtUtc > prompt.UpdatedAtUtc
            ? workflow.UpdatedAtUtc
            : prompt.UpdatedAtUtc;

        return new TaskSummaryDto(
            prompt.Id,
            prompt.WorkingDirectoryId,
            workingDirectoryName,
            prompt.Title,
            prompt.Status,
            workflow?.Status,
            workflow?.CurrentPhaseId,
            workflow?.CurrentPhaseName,
            workflow?.CurrentPhaseColor,
            workflow?.CurrentActor,
            workflow?.EnteredCurrentPhaseAtUtc,
            updatedAtUtc,
            hasChildPrompts,
            hasLinkedPlan,
            workflow is null ? null : workflow.RowVersion.ToString(CultureInfo.InvariantCulture));
    }
}
