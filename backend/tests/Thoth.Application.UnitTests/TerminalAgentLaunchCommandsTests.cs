using System.Text;
using FluentAssertions;
using Thoth.Application.Features.Terminals;

namespace Thoth.Application.UnitTests;

public sealed class TerminalAgentLaunchCommandsTests
{
    [Theory]
    [InlineData(TerminalAgentLaunch.Claude, "claude --dangerously-skip-permissions --effort max\r")]
    [InlineData(TerminalAgentLaunch.Codex, "codex --yolo\r")]
    [InlineData(TerminalAgentLaunch.Grok, "grok --always-approve\r")]
    public void ResolveInitialInput_maps_known_agents(TerminalAgentLaunch agent, string expected)
    {
        var input = TerminalAgentLaunchCommands.ResolveInitialInput(agent);

        input.Should().NotBeNull();
        Encoding.UTF8.GetString(input!).Should().Be(expected);
    }

    [Theory]
    [InlineData("Claude", TerminalAgentLaunch.Claude)]
    [InlineData("ClaudePlan", TerminalAgentLaunch.ClaudePlan)]
    [InlineData("codex", TerminalAgentLaunch.Codex)]
    [InlineData("GROK", TerminalAgentLaunch.Grok)]
    public void TryParse_accepts_supported_names(string value, TerminalAgentLaunch expected)
    {
        TerminalAgentLaunchCommands.TryParse(value, out var agent).Should().BeTrue();
        agent.Should().Be(expected);
    }

    [Fact]
    public void ResolveInitialInput_for_claude_plan_includes_plan_mode_and_prompt()
    {
        const string prompt = "Planeje a feature\r\ncom \"aspas\" e café ☕";

        var input = TerminalAgentLaunchCommands.ResolveInitialInput(TerminalAgentLaunch.ClaudePlan, prompt);

        input.Should().NotBeNull();
        var command = Encoding.UTF8.GetString(input!);
        command.Should().StartWith("$p = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('");
        command.Should().Contain("claude --effort max --permission-mode plan $p\r");
        command.Should().NotContain("--dangerously-skip-permissions");

        var base64 = ExtractBase64Payload(command);
        Encoding.UTF8.GetString(Convert.FromBase64String(base64)).Should().Be(prompt);
    }

    [Fact]
    public void ResolveInitialInput_for_claude_plan_handles_empty_prompt()
    {
        var input = TerminalAgentLaunchCommands.ResolveInitialInput(TerminalAgentLaunch.ClaudePlan, string.Empty);

        input.Should().NotBeNull();
        var command = Encoding.UTF8.GetString(input!);
        command.Should().Contain("claude --effort max --permission-mode plan $p\r");
        command.Should().NotContain("--dangerously-skip-permissions");
        var base64 = ExtractBase64Payload(command);
        Encoding.UTF8.GetString(Convert.FromBase64String(base64)).Should().BeEmpty();
    }

    private static string ExtractBase64Payload(string command)
    {
        const string prefix = "[Convert]::FromBase64String('";
        var start = command.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
        var end = command.IndexOf("')", start, StringComparison.Ordinal);
        return command[start..end];
    }
}