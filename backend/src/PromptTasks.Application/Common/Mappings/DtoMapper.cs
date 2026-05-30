using System.Globalization;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;
using PromptTasks.Domain.WorkingDirectories;

namespace PromptTasks.Application.Common.Mappings;

public static class DtoMapper
{
    public static WorkingDirectoryDto ToDto(this WorkingDirectory workingDirectory) =>
        new(
            workingDirectory.Id,
            workingDirectory.Name,
            workingDirectory.AbsolutePath,
            workingDirectory.RespectGitignore,
            workingDirectory.CreatedAtUtc,
            workingDirectory.UpdatedAtUtc);

    public static PromptDto ToDto(this Prompt prompt, IEnumerable<PromptFileReference> references) =>
        new(
            prompt.Id,
            prompt.WorkingDirectoryId,
            prompt.Title,
            prompt.Content,
            prompt.TargetAgent,
            prompt.Kind,
            prompt.Status,
            prompt.CurrentVersion,
            prompt.RowVersion.ToString(CultureInfo.InvariantCulture),
            prompt.CreatedAtUtc,
            prompt.UpdatedAtUtc,
            references
                .OrderBy(reference => reference.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Select(reference => new FileMentionDto(reference.RelativePath, reference.RawMention))
                .ToList());

    public static PromptVersionDto ToDto(this PromptVersion version) =>
        new(
            version.Id,
            version.PromptId,
            version.VersionNumber,
            version.Title,
            version.Content,
            version.TargetAgent,
            version.Kind,
            version.Status,
            version.ChangeNote,
            version.CreatedAtUtc);
}
