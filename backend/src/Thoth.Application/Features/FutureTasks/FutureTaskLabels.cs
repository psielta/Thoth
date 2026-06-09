namespace Thoth.Application.Features.FutureTasks;

public static class FutureTaskLabels
{
    public static readonly IReadOnlyList<string> Allowed = new[]
    {
        "backend",
        "frontend",
        "database",
        "devops",
        "ai",
        "priority:high"
    };

    public static bool IsAllowed(string label) =>
        Allowed.Contains(label, StringComparer.OrdinalIgnoreCase);
}
