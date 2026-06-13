using FluentAssertions;
using Thoth.Infrastructure.Terminals;

namespace Thoth.Infrastructure.UnitTests;

public sealed class TerminalEnvironmentBootstrapTests
{
    [Fact]
    public void BuildColorOverrides_sets_terminal_color_variables()
    {
        var environment = TerminalEnvironmentBootstrap.BuildColorOverrides();

        environment.Should().ContainKey("TERM").WhoseValue.Should().Be("xterm-256color");
        environment.Should().ContainKey("COLORTERM").WhoseValue.Should().Be("truecolor");
        environment.Should().ContainKey("FORCE_COLOR").WhoseValue.Should().Be("1");
        environment["NO_COLOR"].Should().BeEmpty();
    }

    [Fact]
    public void BuildSpawnEnvironment_preserves_process_path_and_applies_color_overrides()
    {
        const string marker = "THOTH_TERMINAL_PATH_TEST";
        var previousPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", $"{marker};{previousPath}");

        try
        {
            var environment = TerminalEnvironmentBootstrap.BuildSpawnEnvironment();

            environment.Should().ContainKey("PATH");
            environment["PATH"].Should().Contain(marker);
            environment.Should().ContainKey("TERM").WhoseValue.Should().Be("xterm-256color");
            environment.Should().ContainKey("FORCE_COLOR").WhoseValue.Should().Be("1");
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", previousPath);
        }
    }
}