namespace Thoth.Infrastructure.Terminals;

public interface IPtyConnection : IAsyncDisposable
{
    int ProcessId { get; }

    Stream ReaderStream { get; }

    Stream WriterStream { get; }

    event EventHandler<int>? Exited;

    void Resize(int cols, int rows);

    void Kill();
}