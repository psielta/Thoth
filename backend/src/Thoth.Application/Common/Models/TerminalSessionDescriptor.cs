namespace Thoth.Application.Common.Models;

public sealed record TerminalSessionDescriptor(
    Guid Id,
    Guid? PromptId,
    string Shell,
    string Cwd,
    DateTimeOffset CreatedAtUtc);