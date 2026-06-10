namespace Thoth.Application.Common.Models;

public sealed record GitCommandResult(int ExitCode, string StandardOutput, string StandardError);
