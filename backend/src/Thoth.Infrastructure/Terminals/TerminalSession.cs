namespace Thoth.Infrastructure.Terminals;

internal sealed class TerminalSession
{
    public required Guid Id { get; init; }
    public required Guid PromptId { get; init; }
    public required string Shell { get; init; }
    public required string Cwd { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required IPtyConnection Pty { get; init; }
    public object Gate { get; } = new();
    public HashSet<string> Connections { get; } = new(StringComparer.Ordinal);
    public DateTimeOffset LastActivityUtc { get; set; }
    public CancellationTokenSource OutputPumpCts { get; } = new();
    public List<byte> OutputBuffer { get; } = [];
    public Timer? FlushTimer { get; set; }
}