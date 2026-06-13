using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Interfaces;

public interface ITerminalSessionCoordinator
{
    Task<TerminalSessionDescriptor> CreateAsync(
        Guid promptId,
        string cwd,
        string shell,
        byte[]? initialInput,
        CancellationToken cancellationToken,
        byte[]? followUpInput = null);

    void WriteInput(Guid sessionId, byte[] input);

    void Resize(Guid sessionId, ushort cols, ushort rows);

    Task CloseAsync(Guid sessionId, CancellationToken cancellationToken);

    void AttachConnection(Guid sessionId, string connectionId);

    void DetachConnection(Guid sessionId, string connectionId);

    void ReleaseConnection(string connectionId);

    IReadOnlyList<TerminalSessionDescriptor> ListForPrompt(Guid promptId);

    IReadOnlyList<TerminalSessionDescriptor> ListAll();

    TerminalSessionDescriptor? TryGetSession(Guid sessionId);

    Task KillForPromptAsync(Guid promptId, CancellationToken cancellationToken);
}