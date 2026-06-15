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

    public static byte[]? ResolveFollowUpInput(
        TerminalAgentLaunch? agent,
        string? promptContent = null,
        bool submitPrompt = false)
    {
        if (agent is null)
        {
            return null;
        }

        // ClaudePlan sempre envia o prompt apos lancar; os demais agentes (Claude/Codex/Grok)
        // so enviam o conteudo quando explicitamente solicitado (submitPrompt).
        var shouldSubmit = agent == TerminalAgentLaunch.ClaudePlan || submitPrompt;
        if (!shouldSubmit)
        {
            return null;
        }

        var submission = BuildPromptSubmission(promptContent);
        return submission is null ? null : Encoding.UTF8.GetBytes(submission);
    }

    public static TerminalStagedInitialInput? ResolveClaudePlanStagedInput(string? promptContent)
    {
        var followUp = BuildPromptSubmission(promptContent);
        return new TerminalStagedInitialInput(
            Encoding.UTF8.GetBytes(BuildClaudePlanLaunchPowerShellCommand()),
            followUp is null ? null : Encoding.UTF8.GetBytes(followUp));
    }

    internal static string BuildClaudePlanLaunchPowerShellCommand()
    {
        return $"claude {ClaudeBaseFlags}\r";
    }

    internal static string? BuildPromptSubmission(string? promptContent)
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
