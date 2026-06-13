using PortaPtyConnection = Porta.Pty.IPtyConnection;

namespace Thoth.Infrastructure.Terminals;

internal sealed class PortaPtyConnectionAdapter : IPtyConnection
{
    private readonly PortaPtyConnection _inner;
    private EventHandler<int>? _exited;

    public PortaPtyConnectionAdapter(PortaPtyConnection inner)
    {
        _inner = inner;
        _inner.ProcessExited += OnProcessExited;
    }

    public int ProcessId => _inner.Pid;

    public Stream ReaderStream => _inner.ReaderStream;

    public Stream WriterStream => _inner.WriterStream;

    public event EventHandler<int>? Exited
    {
        add => _exited += value;
        remove => _exited -= value;
    }

    public void Resize(int cols, int rows) => _inner.Resize(cols, rows);

    public void Kill() => _inner.Kill();

    public ValueTask DisposeAsync()
    {
        _inner.ProcessExited -= OnProcessExited;
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    private void OnProcessExited(object? sender, EventArgs args)
    {
        var exitCode = args.GetType().GetProperty("ExitCode")?.GetValue(args) as int? ?? 0;
        _exited?.Invoke(this, exitCode);
    }
}