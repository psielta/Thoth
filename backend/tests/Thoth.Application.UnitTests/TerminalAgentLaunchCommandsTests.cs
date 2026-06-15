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
    public void ResolveClaudePlanStagedInput_launches_bypass_claude_and_prefills_plain_prompt()
    {
        const string prompt = "Planeje a feature\r\ncom \"aspas\" e café ☕";

        var staged = TerminalAgentLaunchCommands.ResolveClaudePlanStagedInput(prompt);

        staged.Should().NotBeNull();
        var launch = Encoding.UTF8.GetString(staged!.Launch);
        launch.Should().Be("claude --dangerously-skip-permissions --effort max\r");
        launch.Should().NotContain("--permission-mode plan");
        launch.Should().NotContain("--settings");

        var followUp = Encoding.UTF8.GetString(staged.FollowUp!);
        followUp.Should().Be("Planeje a feature com \"aspas\" e café ☕");
        followUp.Should().NotStartWith("/plan");
    }

    [Fact]
    public void ResolveFollowUpInput_for_claude_plan_skips_empty_prompt()
    {
        TerminalAgentLaunchCommands.ResolveFollowUpInput(TerminalAgentLaunch.ClaudePlan, string.Empty)
            .Should().BeNull();
    }

    [Fact]
    public void ResolveFollowUpInput_for_claude_plan_prefills_without_submitting()
    {
        var followUp = TerminalAgentLaunchCommands.ResolveFollowUpInput(
            TerminalAgentLaunch.ClaudePlan,
            "Planeje\r\nsem executar");

        followUp.Should().NotBeNull();
        Encoding.UTF8.GetString(followUp!).Should().Be("Planeje sem executar");
    }

    [Fact]
    public void FlattenPromptForClaudeCli_collapses_newlines_to_spaces()
    {
        TerminalAgentLaunchCommands.FlattenPromptForClaudeCli("linha1\nlinha2").Should().Be("linha1 linha2");
    }

    [Theory]
    [InlineData(TerminalAgentLaunch.Claude)]
    [InlineData(TerminalAgentLaunch.Codex)]
    [InlineData(TerminalAgentLaunch.Grok)]
    public void ResolveFollowUpInput_submits_flattened_prompt_for_executor_agents(TerminalAgentLaunch agent)
    {
        var followUp = TerminalAgentLaunchCommands.ResolveFollowUpInput(
            agent,
            "Revise a PR\r\ncom cuidado",
            submitPrompt: true);

        followUp.Should().NotBeNull();
        Encoding.UTF8.GetString(followUp!).Should().Be("Revise a PR com cuidado\r");
    }

    [Theory]
    [InlineData(TerminalAgentLaunch.Claude)]
    [InlineData(TerminalAgentLaunch.Codex)]
    [InlineData(TerminalAgentLaunch.Grok)]
    public void ResolveFollowUpInput_without_submit_has_no_follow_up(TerminalAgentLaunch agent)
    {
        TerminalAgentLaunchCommands.ResolveFollowUpInput(agent, "Revise a PR").Should().BeNull();
    }

    [Fact]
    public void ResolveFollowUpInput_submit_skips_empty_prompt()
    {
        TerminalAgentLaunchCommands.ResolveFollowUpInput(TerminalAgentLaunch.Claude, "   ", submitPrompt: true)
            .Should().BeNull();
    }
}
