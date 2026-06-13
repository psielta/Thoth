using FluentAssertions;
using Thoth.Infrastructure.Terminals;

namespace Thoth.Infrastructure.UnitTests;

public sealed class TerminalEnvironmentBootstrapTests
{
    [Fact]
    public void BuildColorEnvironment_sets_terminal_color_variables()
    {
        var environment = TerminalEnvironmentBootstrap.BuildColorEnvironment();

        environment.Should().ContainKey("TERM").WhoseValue.Should().Be("xterm-256color");
        environment.Should().ContainKey("COLORTERM").WhoseValue.Should().Be("truecolor");
        environment.Should().ContainKey("FORCE_COLOR").WhoseValue.Should().Be("1");
        environment["NO_COLOR"].Should().BeEmpty();
    }
}