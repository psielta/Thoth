using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Infrastructure.Terminals;

namespace Thoth.Infrastructure.UnitTests;

public sealed class TerminalSessionManagerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"prompttasks-terminal-{Guid.NewGuid():N}");
    private readonly FakePtyConnectionFactory _ptyFactory = new();
    private readonly RecordingTerminalNotifier _notifier = new();
    private readonly TerminalSessionManager _manager;

    public TerminalSessionManagerTests()
    {
        Directory.CreateDirectory(_root);

        var services = new ServiceCollection();
        services.AddScoped<ITerminalNotifier>(_ => _notifier);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        _manager = new TerminalSessionManager(
            scopeFactory,
            _ptyFactory,
            Options.Create(new TerminalOptions
            {
                Enabled = true,
                MaxSessionsPerPrompt = 2,
                MaxTotalSessions = 4,
                OrphanTimeoutSeconds = 4,
                OutputFlushMilliseconds = 10,
                MaxOutputChunkBytes = 1024,
                MaxOutputHistoryBytes = 8
            }),
            NullLogger<TerminalSessionManager>.Instance);
    }

    [Fact]
    public async Task CreateAsync_registers_session_with_descriptor()
    {
        var promptId = Guid.CreateVersion7();
        var descriptor = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);

        descriptor.PromptId.Should().Be(promptId);
        descriptor.Cwd.Should().Be(Path.GetFullPath(_root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        descriptor.Shell.ToLowerInvariant().Should().EndWith("powershell.exe");
        _manager.ListForPrompt(promptId).Should().ContainSingle(item => item.Id == descriptor.Id);
    }

    [Fact]
    public async Task CreateGenericAsync_registers_ownerless_session_listed_for_owner()
    {
        var ownerId = Guid.CreateVersion7();
        var descriptor = await _manager.CreateGenericAsync(ownerId, _root, string.Empty, null, CancellationToken.None);

        descriptor.PromptId.Should().BeNull();
        descriptor.Cwd.Should().Be(
            Path.GetFullPath(_root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        _manager.ListForOwner(ownerId).Should().ContainSingle(item => item.Id == descriptor.Id);
    }

    [Fact]
    public async Task ListForOwner_excludes_other_owners_and_prompt_sessions()
    {
        var ownerId = Guid.CreateVersion7();
        var otherOwnerId = Guid.CreateVersion7();
        var promptId = Guid.CreateVersion7();

        var mine = await _manager.CreateGenericAsync(ownerId, _root, string.Empty, null, CancellationToken.None);
        await _manager.CreateGenericAsync(otherOwnerId, _root, string.Empty, null, CancellationToken.None);
        await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);

        _manager.ListForOwner(ownerId).Should().ContainSingle().Which.Id.Should().Be(mine.Id);
    }

    [Fact]
    public async Task CreateAsync_delivers_initial_input_to_pty()
    {
        var promptId = Guid.CreateVersion7();
        var initialInput = "codex --yolo\r"u8.ToArray();

        await _manager.CreateAsync(promptId, _root, string.Empty, initialInput, CancellationToken.None);
        await Task.Delay(700);

        _ptyFactory.LastWritten.Should().BeEquivalentTo(initialInput);
    }

    [Fact]
    public async Task WriteInput_routes_bytes_to_pty()
    {
        var promptId = Guid.CreateVersion7();
        var descriptor = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);
        var input = "echo hi"u8.ToArray();

        _manager.WriteInput(descriptor.Id, input);

        _ptyFactory.LastWritten.Should().BeEquivalentTo(input);
    }

    [Fact]
    public async Task GetOutputHistory_returns_bytes_already_read_from_pty()
    {
        var promptId = Guid.CreateVersion7();
        var descriptor = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);
        var output = "abcdef"u8.ToArray();

        _ptyFactory.LastConnection!.EmitOutput(output);

        var history = await WaitForHistoryAsync(descriptor.Id, output.Length);
        history.StartOffset.Should().Be(0);
        history.EndOffset.Should().Be(output.Length);
        history.IsTruncated.Should().BeFalse();
        Convert.FromBase64String(history.DataBase64).Should().BeEquivalentTo(output);
    }

    [Fact]
    public async Task GetOutputHistory_truncates_oldest_bytes_when_limit_is_exceeded()
    {
        var promptId = Guid.CreateVersion7();
        var descriptor = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);
        var output = "1234567890"u8.ToArray();

        _ptyFactory.LastConnection!.EmitOutput(output);

        var history = await WaitForHistoryAsync(descriptor.Id, output.Length);
        history.StartOffset.Should().Be(2);
        history.EndOffset.Should().Be(10);
        history.IsTruncated.Should().BeTrue();
        Convert.FromBase64String(history.DataBase64).Should().BeEquivalentTo("34567890"u8.ToArray());
    }

    [Fact]
    public async Task GetOutputHistory_keeps_offsets_when_history_is_disabled()
    {
        var ptyFactory = new FakePtyConnectionFactory();
        var notifier = new RecordingTerminalNotifier();
        using var manager = CreateManager(ptyFactory, notifier, maxOutputHistoryBytes: 0);
        var promptId = Guid.CreateVersion7();
        var descriptor = await manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);
        var output = "abcdef"u8.ToArray();

        ptyFactory.LastConnection!.EmitOutput(output);

        var history = await WaitForHistoryAsync(manager, descriptor.Id, output.Length);
        history.StartOffset.Should().Be(output.Length);
        history.EndOffset.Should().Be(output.Length);
        history.IsTruncated.Should().BeTrue();
        Convert.FromBase64String(history.DataBase64).Should().BeEmpty();
    }

    [Fact]
    public async Task GetOutputHistory_advances_start_offset_across_multiple_outputs()
    {
        var promptId = Guid.CreateVersion7();
        var descriptor = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);

        _ptyFactory.LastConnection!.EmitOutput("12345"u8.ToArray());
        _ptyFactory.LastConnection!.EmitOutput("67890"u8.ToArray());

        var history = await WaitForHistoryAsync(descriptor.Id, 10);
        history.StartOffset.Should().Be(2);
        history.EndOffset.Should().Be(10);
        history.IsTruncated.Should().BeTrue();
        Convert.FromBase64String(history.DataBase64).Should().BeEquivalentTo("34567890"u8.ToArray());
    }

    [Fact]
    public async Task ProcessOutputQueueAsync_notifies_output_with_start_offset()
    {
        await _manager.StartAsync(CancellationToken.None);

        try
        {
            var promptId = Guid.CreateVersion7();
            var descriptor = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);
            var output = "hello"u8.ToArray();

            _ptyFactory.LastConnection!.EmitOutput(output);

            var notified = await WaitForOutputNotificationAsync(descriptor.Id);
            notified.StartOffset.Should().Be(0);
            Convert.FromBase64String(notified.DataBase64).Should().BeEquivalentTo(output);
        }
        finally
        {
            await _manager.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task CloseAsync_notifies_exit_to_clients()
    {
        await _manager.StartAsync(CancellationToken.None);

        try
        {
            var promptId = Guid.CreateVersion7();
            var descriptor = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);
            await _manager.CloseAsync(descriptor.Id, CancellationToken.None);
            await Task.Delay(200);

            _notifier.Exits.Should().ContainSingle(item =>
                item.SessionId == descriptor.Id && item.ExitCode == -1);
        }
        finally
        {
            await _manager.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task KillForPromptAsync_removes_all_prompt_sessions()
    {
        var promptId = Guid.CreateVersion7();
        var first = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);
        var second = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);

        await _manager.KillForPromptAsync(promptId, CancellationToken.None);

        _manager.TryGetSession(first.Id).Should().BeNull();
        _manager.TryGetSession(second.Id).Should().BeNull();
        _manager.ListForPrompt(promptId).Should().BeEmpty();
        _ptyFactory.KilledCount.Should().Be(2);
    }

    [Fact]
    public async Task ReleaseConnection_detaches_without_killing_session()
    {
        var promptId = Guid.CreateVersion7();
        var descriptor = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);
        _manager.AttachConnection(descriptor.Id, "conn-1");
        _manager.ReleaseConnection("conn-1");

        _manager.TryGetSession(descriptor.Id).Should().NotBeNull();
        _ptyFactory.KilledCount.Should().Be(0);
    }

    [Fact]
    public async Task ReapOrphansAsync_kills_unattached_session_after_timeout()
    {
        await _manager.StartAsync(CancellationToken.None);

        try
        {
            var promptId = Guid.CreateVersion7();
            var descriptor = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);

            await Task.Delay(11_500);

            _manager.TryGetSession(descriptor.Id).Should().BeNull();
            _ptyFactory.KilledCount.Should().Be(1);
        }
        finally
        {
            await _manager.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ReapOrphansAsync_keeps_session_with_attached_connection()
    {
        await _manager.StartAsync(CancellationToken.None);

        try
        {
            var promptId = Guid.CreateVersion7();
            var descriptor = await _manager.CreateAsync(promptId, _root, string.Empty, null, CancellationToken.None);
            _manager.AttachConnection(descriptor.Id, "conn-1");

            await Task.Delay(11_500);

            _manager.TryGetSession(descriptor.Id).Should().NotBeNull();
            _ptyFactory.KilledCount.Should().Be(0);
        }
        finally
        {
            await _manager.StopAsync(CancellationToken.None);
        }
    }

    private Task<TerminalOutputHistoryDto> WaitForHistoryAsync(Guid sessionId, long minEndOffset) =>
        WaitForHistoryAsync(_manager, sessionId, minEndOffset);

    private static async Task<TerminalOutputHistoryDto> WaitForHistoryAsync(
        TerminalSessionManager manager,
        Guid sessionId,
        long minEndOffset)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var history = manager.GetOutputHistory(sessionId);
            if (history is not null && history.EndOffset >= minEndOffset)
            {
                return history;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Terminal output history was not populated in time.");
    }

    private static TerminalSessionManager CreateManager(
        FakePtyConnectionFactory ptyFactory,
        RecordingTerminalNotifier notifier,
        int maxOutputHistoryBytes)
    {
        var services = new ServiceCollection();
        services.AddScoped<ITerminalNotifier>(_ => notifier);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        return new TerminalSessionManager(
            scopeFactory,
            ptyFactory,
            Options.Create(new TerminalOptions
            {
                Enabled = true,
                MaxSessionsPerPrompt = 2,
                MaxTotalSessions = 4,
                OrphanTimeoutSeconds = 4,
                OutputFlushMilliseconds = 10,
                MaxOutputChunkBytes = 1024,
                MaxOutputHistoryBytes = maxOutputHistoryBytes
            }),
            NullLogger<TerminalSessionManager>.Instance);
    }

    private async Task<(Guid SessionId, long StartOffset, string DataBase64)> WaitForOutputNotificationAsync(Guid sessionId)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (_notifier.Outputs.TryDequeue(out var output) && output.SessionId == sessionId)
            {
                return output;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Terminal output notification was not emitted in time.");
    }

    public void Dispose()
    {
        _manager.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class FakePtyConnectionFactory : IPtyConnectionFactory
    {
        public byte[] LastWritten { get; set; } = [];
        public int KilledCount { get; set; }
        public FakePtyConnection? LastConnection { get; private set; }

        public Task<IPtyConnection> CreateAsync(
            string shell,
            string cwd,
            int cols,
            int rows,
            CancellationToken cancellationToken)
        {
            var connection = new FakePtyConnection(this);
            LastConnection = connection;
            return Task.FromResult<IPtyConnection>(connection);
        }
    }

    private sealed class FakePtyConnection(FakePtyConnectionFactory factory) : IPtyConnection
    {
        private readonly ProducerConsumerStream _reader = new();
        private readonly CapturingWriteStream _writer = new(factory);
        private bool _killed;

        public int ProcessId => 4242;

        public Stream ReaderStream => _reader;

        public Stream WriterStream => _writer;

        public event EventHandler<int>? Exited;

        public void Resize(int cols, int rows)
        {
        }

        public void Kill()
        {
            if (_killed)
            {
                return;
            }

            _killed = true;
            factory.KilledCount++;
            Exited?.Invoke(this, 0);
        }

        public void EmitOutput(byte[] data) => _reader.WriteOutput(data);

        public ValueTask DisposeAsync()
        {
            _reader.Complete();
            _reader.Dispose();
            _writer.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ProducerConsumerStream : Stream
    {
        private readonly SemaphoreSlim _signal = new(0);
        private readonly Queue<byte> _buffer = new();
        private bool _completed;
        private bool _disposed;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public void WriteOutput(byte[] data)
        {
            lock (_buffer)
            {
                if (_disposed)
                {
                    return;
                }

                foreach (var item in data)
                {
                    _buffer.Enqueue(item);
                }
            }

            _signal.Release();
        }

        public void Complete()
        {
            lock (_buffer)
            {
                if (_completed || _disposed)
                {
                    return;
                }

                _completed = true;
            }

            _signal.Release();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                lock (_buffer)
                {
                    if (_buffer.Count > 0)
                    {
                        var count = Math.Min(destination.Length, _buffer.Count);
                        for (var index = 0; index < count; index++)
                        {
                            destination.Span[index] = _buffer.Dequeue();
                        }

                        return count;
                    }

                    if (_completed)
                    {
                        return 0;
                    }
                }

                await _signal.WaitAsync(cancellationToken);
            }
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_buffer)
                {
                    _completed = true;
                    _disposed = true;
                }

                _signal.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class CapturingWriteStream(FakePtyConnectionFactory factory) : Stream
    {
        private readonly MemoryStream _inner = new();

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            factory.LastWritten = _inner.ToArray();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class RecordingTerminalNotifier : ITerminalNotifier
    {
        public ConcurrentQueue<(Guid SessionId, long StartOffset, string DataBase64)> Outputs { get; } = new();
        public ConcurrentQueue<(Guid SessionId, int ExitCode)> Exits { get; } = new();

        public Task TerminalOutputAsync(Guid sessionId, long startOffset, string dataBase64, CancellationToken cancellationToken)
        {
            Outputs.Enqueue((sessionId, startOffset, dataBase64));
            return Task.CompletedTask;
        }

        public Task TerminalExitedAsync(Guid sessionId, int exitCode, CancellationToken cancellationToken)
        {
            Exits.Enqueue((sessionId, exitCode));
            return Task.CompletedTask;
        }
    }
}
