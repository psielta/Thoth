using System.Text;

namespace Thoth.Application.Features.Terminals;

public enum TerminalAgentLaunch
{
    Claude,
    ClaudePlan,
    Codex,
    Grok,
}

public static class TerminalAgentLaunchCommands
{
    private const string ClaudeBaseFlags = "--dangerously-skip-permissions --effort max";
    private const string ClaudePlanFlags = "--effort max --permission-mode plan";

    public static bool TryParse(string? value, out TerminalAgentLaunch agent)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            agent = default;
            return false;
        }

        return Enum.TryParse(value, ignoreCase: true, out agent);
    }

    public static byte[]? ResolveInitialInput(TerminalAgentLaunch? agent, string? promptContent = null)
    {
        var command = agent switch
        {
            TerminalAgentLaunch.Claude => $"claude {ClaudeBaseFlags}\r",
            TerminalAgentLaunch.ClaudePlan => BuildClaudePlanPowerShellCommand(promptContent),
            TerminalAgentLaunch.Codex => "codex --yolo\r",
            TerminalAgentLaunch.Grok => "grok --always-approve\r",
            _ => null,
        };

        return command is null ? null : Encoding.UTF8.GetBytes(command);
    }

    internal static string BuildClaudePlanPowerShellCommand(string? promptContent)
    {
        var content = promptContent ?? string.Empty;
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        return
            $"$p = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{base64}')); claude {ClaudePlanFlags} $p\r";
    }
}