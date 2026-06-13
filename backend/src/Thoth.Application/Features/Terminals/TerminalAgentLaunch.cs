using System.Text;
using System.Text.RegularExpressions;

namespace Thoth.Application.Features.Terminals;

public enum TerminalAgentLaunch
{
    Claude,
    ClaudePlan,
    Codex,
    Grok,
}

public sealed record TerminalStagedInitialInput(byte[] Launch, byte[]? FollowUp = null);

public static class TerminalAgentLaunchCommands
{
    private const string ClaudeBaseFlags = "--dangerously-skip-permissions --effort max";
    private const string ClaudePlanFlags = "--effort max --permission-mode plan";
    private const string ClaudePlanSettingsJson =
        """{"permissions":{"defaultMode":"plan","allow":["Read","Glob","Grep","LS","WebFetch","WebSearch","Task","Skill","Agent(Plan)","NotebookRead","TodoRead","Bash"]}}""";

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
        if (agent is null)
        {
            return null;
        }

        if (agent == TerminalAgentLaunch.ClaudePlan)
        {
            var staged = ResolveClaudePlanStagedInput(promptContent);
            return staged?.Launch;
        }

        var command = agent switch
        {
            TerminalAgentLaunch.Claude => $"claude {ClaudeBaseFlags}\r",
            TerminalAgentLaunch.Codex => "codex --yolo\r",
            TerminalAgentLaunch.Grok => "grok --always-approve\r",
            _ => null,
        };

        return command is null ? null : Encoding.UTF8.GetBytes(command);
    }

    public static byte[]? ResolveFollowUpInput(TerminalAgentLaunch? agent, string? promptContent = null)
    {
        if (agent != TerminalAgentLaunch.ClaudePlan)
        {
            return null;
        }

        return ResolveClaudePlanStagedInput(promptContent)?.FollowUp;
    }

    public static TerminalStagedInitialInput? ResolveClaudePlanStagedInput(string? promptContent)
    {
        var followUp = BuildClaudePlanPromptSubmission(promptContent);
        return new TerminalStagedInitialInput(
            Encoding.UTF8.GetBytes(BuildClaudePlanLaunchPowerShellCommand()),
            followUp is null ? null : Encoding.UTF8.GetBytes(followUp));
    }

    internal static string BuildClaudePlanLaunchPowerShellCommand()
    {
        var settingsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ClaudePlanSettingsJson));
        return
            "$s = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + settingsBase64 + "')); " +
            "$p = Join-Path $env:TEMP ('thoth-claude-plan-{0}.json' -f [guid]::NewGuid().ToString()); " +
            "[System.IO.File]::WriteAllText($p, $s, [System.Text.UTF8Encoding]::new($false)); " +
            $"claude {ClaudePlanFlags} --settings $p\r";
    }

    internal static string? BuildClaudePlanPromptSubmission(string? promptContent)
    {
        var flattened = FlattenPromptForClaudeCli(promptContent);
        return string.IsNullOrEmpty(flattened) ? null : $"{flattened}\r";
    }

    public static string FlattenPromptForClaudeCli(string? promptContent)
    {
        if (string.IsNullOrWhiteSpace(promptContent))
        {
            return string.Empty;
        }

        return Regex.Replace(promptContent, "\r\n|\r|\n", " ").Trim();
    }
}