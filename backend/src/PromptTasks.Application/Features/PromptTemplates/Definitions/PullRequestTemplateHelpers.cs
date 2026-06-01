namespace PromptTasks.Application.Features.PromptTemplates.Definitions;

internal static class PullRequestTemplateHelpers
{
    public static string FormatPullRequestReference(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.StartsWith('#') ||
            normalized.StartsWith("PR ", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return $"PR #{normalized}";
    }
}
