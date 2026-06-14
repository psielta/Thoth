namespace Thoth.Application.Common.Interfaces;

public interface ITerminalNotifier
{
    Task TerminalOutputAsync(Guid sessionId, long startOffset, string dataBase64, CancellationToken cancellationToken);
    Task TerminalExitedAsync(Guid sessionId, int exitCode, CancellationToken cancellationToken);
}
