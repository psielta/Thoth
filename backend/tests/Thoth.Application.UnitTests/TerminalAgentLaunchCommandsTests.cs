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
    public void ResolveClaudePlanStagedInput_launches_with_settings_and_plan_prefixed_prompt()
    {
        const string prompt = "Planeje a feature\r\ncom \"aspas\" e café ☕";

        var staged = TerminalAgentLaunchCommands.ResolveClaudePlanStagedInput(prompt);

        staged.Should().NotBeNull();
        var launch = Encoding.UTF8.GetString(staged!.Launch);
        launch.Should().Contain("[System.IO.File]::WriteAllText($p, $s");
        launch.Should().Contain("claude --effort max --permission-mode plan --settings $p\r");
        launch.Should().NotContain("--settings $s");
        launch.Should().NotContain("--dangerously-skip-permissions");
        launch.Should().Contain(Convert.ToBase64String(Encoding.UTF8.GetBytes(
            """{"permissions":{"defaultMode":"plan","allow":["Read","Glob","Grep","LS","WebFetch","WebSearch","Task","Skill","Agent(Plan)","NotebookRead","TodoRead","Bash"]}}""")));

        var followUp = Encoding.UTF8.GetString(staged.FollowUp!);
        followUp.Should().Be("/plan Planeje a feature com \"aspas\" e café ☕\r");
    }

    [Fact]
    public void ResolveFollowUpInput_for_claude_plan_handles_empty_prompt()
    {
        var followUp = TerminalAgentLaunchCommands.ResolveFollowUpInput(TerminalAgentLaunch.ClaudePlan, string.Empty);

        followUp.Should().NotBeNull();
        Encoding.UTF8.GetString(followUp!).Should().Be("/plan\r");
    }

    [Fact]
    public void FlattenPromptForClaudeCli_collapses_newlines_to_spaces()
    {
        TerminalAgentLaunchCommands.FlattenPromptForClaudeCli("linha1\nlinha2").Should().Be("linha1 linha2");
    }
}