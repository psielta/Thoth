namespace Thoth.Infrastructure.Terminals;

public static class TerminalEnvironmentBootstrap
{
    public static Dictionary<string, string> BuildColorEnvironment()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TERM"] = "xterm-256color",
            ["COLORTERM"] = "truecolor",
            ["FORCE_COLOR"] = "1",
            ["CLICOLOR"] = "1",
            ["CLICOLOR_FORCE"] = "1",
            ["NO_COLOR"] = string.Empty,
        };
    }
}