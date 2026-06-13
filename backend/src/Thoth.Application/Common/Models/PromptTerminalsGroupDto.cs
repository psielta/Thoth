namespace Thoth.Application.Common.Models;

public sealed record PromptTerminalsGroupDto(
    Guid PromptId,
    string PromptTitle,
    Guid WorkingDirectoryId,
    string WorkingDirectoryName,
    bool IsArchived,
    IReadOnlyList<TerminalSessionDescriptor> Terminals);
