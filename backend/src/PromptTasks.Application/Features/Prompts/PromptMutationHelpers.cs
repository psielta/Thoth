using System.Globalization;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;
using PromptTasks.Domain.WorkingDirectories;

namespace PromptTasks.Application.Features.Prompts;

internal static class PromptMutationHelpers
{
    public static WorkingDirectory GetWorkingDirectory(
        IApplicationDbContext context,
        Guid workingDirectoryId,
        Guid ownerId)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == workingDirectoryId && item.OwnerId == ownerId);

        return directory ?? throw new NotFoundException("Working directory was not found.");
    }

    public static Prompt GetPrompt(IApplicationDbContext context, Guid promptId, Guid ownerId)
    {
        var prompt = context.Prompts
            .FirstOrDefault(item => item.Id == promptId && item.OwnerId == ownerId);

        return prompt ?? throw new NotFoundException("Prompt was not found.");
    }

    public static void EnsureRowVersion(Prompt prompt, string rowVersion)
    {
        if (!uint.TryParse(rowVersion, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ||
            parsed != prompt.RowVersion)
        {
            throw new ConflictException("The prompt was changed by another operation. Reload it before saving.");
        }
    }

    public static PromptVersion CreateVersion(Prompt prompt, IDateTimeProvider dateTimeProvider, string? changeNote = null) =>
        new()
        {
            PromptId = prompt.Id,
            VersionNumber = prompt.CurrentVersion,
            Title = prompt.Title,
            Content = prompt.Content,
            TargetAgent = prompt.TargetAgent,
            Kind = prompt.Kind,
            Status = prompt.Status,
            ChangeNote = changeNote,
            CreatedAtUtc = dateTimeProvider.UtcNow
        };

    public static async Task<IReadOnlyList<PromptFileReference>> BuildReferencesAsync(
        IWorkspaceFileService workspaceFileService,
        string rootAbsolutePath,
        IEnumerable<FileMentionDto>? mentions,
        CancellationToken cancellationToken)
    {
        var references = new List<PromptFileReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mention in mentions ?? Array.Empty<FileMentionDto>())
        {
            var relativePath = mention.RelativePath.Trim();
            if (relativePath.Length == 0 || !seen.Add(relativePath))
            {
                continue;
            }

            var resolution = await workspaceFileService.ResolveRelativePathAsync(rootAbsolutePath, relativePath, cancellationToken);
            references.Add(new PromptFileReference
            {
                RelativePath = resolution.RelativePath,
                RawMention = mention.Label?.Trim() is { Length: > 0 } label ? label : resolution.RelativePath,
                Exists = resolution.Exists,
                ResolvedAtUtc = resolution.ResolvedAtUtc
            });
        }

        return references;
    }
}
