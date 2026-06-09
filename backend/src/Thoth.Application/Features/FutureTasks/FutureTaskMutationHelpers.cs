using System.Globalization;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Domain.FutureTasks;

namespace Thoth.Application.Features.FutureTasks;

internal static class FutureTaskMutationHelpers
{
    public static FutureTask GetFutureTask(IApplicationDbContext context, Guid futureTaskId, Guid ownerId)
    {
        var task = context.FutureTasks
            .FirstOrDefault(item => item.Id == futureTaskId && item.OwnerId == ownerId);

        return task ?? throw new NotFoundException("Future task was not found.");
    }

    public static void EnsureRowVersion(FutureTask task, string rowVersion)
    {
        if (!uint.TryParse(rowVersion, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ||
            parsed != task.RowVersion)
        {
            throw new ConflictException("The future task was changed by another operation. Reload it before saving.");
        }
    }

    public static IReadOnlyList<string> NormalizeLabels(IReadOnlyList<string>? labels) =>
        labels is null
            ? Array.Empty<string>()
            : labels
                .Select(label => label.Trim())
                .Where(label => label.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    public static string? NormalizeIssueGithubId(string? issueGithubId) =>
        string.IsNullOrWhiteSpace(issueGithubId) ? null : issueGithubId.Trim();

    public static IReadOnlyList<string> LoadLabels(IApplicationDbContext context, Guid futureTaskId) =>
        context.FutureTaskLabels
            .Where(label => label.FutureTaskId == futureTaskId)
            .Select(label => label.Label)
            .ToList();

    public static int CountLinkedPrompts(IApplicationDbContext context, Guid futureTaskId) =>
        context.Prompts.Count(prompt => prompt.FutureTaskId == futureTaskId);
}
