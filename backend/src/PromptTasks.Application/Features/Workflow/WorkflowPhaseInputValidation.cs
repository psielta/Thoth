using System.Text.RegularExpressions;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Workflow;

internal static partial class WorkflowPhaseInputValidation
{
    public const string OrderMessage = "As fases precisam de OrderIndex contíguo começando em zero, sem repetições.";
    public const string ColorMessage = "A cor precisa ser um hexadecimal válido (ex.: #2563eb).";

    [GeneratedRegex("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex ColorRegex();

    public static bool HasContiguousOrder(IReadOnlyList<WorkflowPhaseInput>? phases)
    {
        if (phases is null || phases.Count == 0)
        {
            return false;
        }

        var orders = phases.Select(phase => phase.OrderIndex).OrderBy(order => order).ToList();
        for (var index = 0; index < orders.Count; index++)
        {
            if (orders[index] != index)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsValidColor(string? color) =>
        !string.IsNullOrWhiteSpace(color) && ColorRegex().IsMatch(color);
}
