using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PromptTasks.Application.Common.Interfaces;

namespace PromptTasks.Infrastructure.FileSystem;

public sealed class WorkspaceFileWatchService(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkspaceFileWatchOptions> options,
    ILogger<WorkspaceFileWatchService> logger)
    : BackgroundService, IWorkspaceFileWatchCoordinator, IDisposable
{
    private readonly Channel<FileChangeNotification> _notificationQueue = Channel.CreateUnbounded<FileChangeNotification>();
    private readonly ConcurrentDictionary<Guid, WorkingDirectoryWatch> _directoryWatches = new();
    private readonly ConcurrentDictionary<FileWatchKey, TrackedFile> _trackedFiles = new();
    private readonly ConcurrentDictionary<string, HashSet<FileWatchKey>> _filesByConnection = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Timer> _debounceTimers = new(StringComparer.Ordinal);

    public async Task JoinFileAsync(
        Guid workingDirectoryId,
        string relativePath,
        string connectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fileKey = WorkspaceFilePath.CreateFileKey(relativePath);
        var normalizedRelativePath = WorkspaceFilePath.NormalizeRelativePath(
            relativePath.Trim().TrimStart('@').Replace('\\', '/'));

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var directory = context.WorkingDirectories.FirstOrDefault(item => item.Id == workingDirectoryId);
        if (directory is null)
        {
            return;
        }

        var rootCanonical = WorkspaceFilePath.CanonicalizeExistingPath(directory.AbsolutePath);
        var tracked = _trackedFiles.GetOrAdd(
            new FileWatchKey(workingDirectoryId, fileKey),
            _ => new TrackedFile(workingDirectoryId, fileKey, normalizedRelativePath, rootCanonical));

        lock (tracked.Gate)
        {
            tracked.Connections.Add(connectionId);
        }

        var connectionFiles = _filesByConnection.GetOrAdd(connectionId, _ => new HashSet<FileWatchKey>());
        lock (connectionFiles)
        {
            connectionFiles.Add(new FileWatchKey(workingDirectoryId, fileKey));
        }

        _directoryWatches.GetOrAdd(
            workingDirectoryId,
            _ => CreateWorkingDirectoryWatch(workingDirectoryId, rootCanonical));
    }

    public Task LeaveFileAsync(
        Guid workingDirectoryId,
        string relativePath,
        string connectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fileKey;
        try
        {
            fileKey = WorkspaceFilePath.CreateFileKey(relativePath);
        }
        catch
        {
            return Task.CompletedTask;
        }

        RemoveConnectionFromFile(new FileWatchKey(workingDirectoryId, fileKey), connectionId);
        return Task.CompletedTask;
    }

    public void ReleaseConnection(string connectionId)
    {
        if (!_filesByConnection.TryRemove(connectionId, out var watchedFiles))
        {
            return;
        }

        List<FileWatchKey> keys;
        lock (watchedFiles)
        {
            keys = watchedFiles.ToList();
        }

        foreach (var key in keys)
        {
            RemoveConnectionFromFile(key, connectionId);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessQueueAsync(stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _notificationQueue.Writer.TryComplete();
        Dispose();
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        foreach (var timer in _debounceTimers.Values)
        {
            timer.Dispose();
        }

        foreach (var directoryWatch in _directoryWatches.Values)
        {
            directoryWatch.Dispose();
        }

        _debounceTimers.Clear();
        _directoryWatches.Clear();
        _trackedFiles.Clear();
        _filesByConnection.Clear();
        base.Dispose();
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in _notificationQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var notifier = scope.ServiceProvider.GetRequiredService<IWorkspaceFileNotifier>();
                await notifier.WorkspaceFileChangedAsync(
                    notification.WorkingDirectoryId,
                    notification.RelativePath,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to notify workspace file change for {WorkingDirectoryId} {RelativePath}",
                    notification.WorkingDirectoryId,
                    notification.RelativePath);
            }
        }
    }

    private WorkingDirectoryWatch CreateWorkingDirectoryWatch(Guid workingDirectoryId, string rootCanonical)
    {
        return new WorkingDirectoryWatch(
            workingDirectoryId,
            rootCanonical,
            path => SchedulePath(workingDirectoryId, rootCanonical, path),
            (oldPath, newPath) =>
            {
                SchedulePath(workingDirectoryId, rootCanonical, oldPath);
                SchedulePath(workingDirectoryId, rootCanonical, newPath);
            },
            exception => HandleWatcherError(workingDirectoryId, exception));
    }

    private void SchedulePath(Guid workingDirectoryId, string rootCanonical, string fullPath)
    {
        string relativePath;
        try
        {
            var candidateCanonical = WorkspaceFilePath.TrimEndingDirectorySeparator(Path.GetFullPath(fullPath));
            WorkspaceFilePath.EnsureContained(rootCanonical, candidateCanonical);
            relativePath = WorkspaceFilePath.NormalizeRelativePath(Path.GetRelativePath(rootCanonical, candidateCanonical));
        }
        catch
        {
            return;
        }

        var fileKey = WorkspaceFilePath.CreateFileKey(relativePath);
        if (!_trackedFiles.TryGetValue(new FileWatchKey(workingDirectoryId, fileKey), out var tracked))
        {
            return;
        }

        ScheduleNotification(workingDirectoryId, fileKey, tracked.NormalizedRelativePath);
    }

    private void ScheduleNotification(Guid workingDirectoryId, string fileKey, string relativePath)
    {
        var timerKey = $"{workingDirectoryId}:{fileKey}";
        var dueTime = TimeSpan.FromMilliseconds(Math.Max(options.Value.DebounceMilliseconds, 100));
        var timer = new Timer(_ =>
        {
            _debounceTimers.TryRemove(timerKey, out var removedTimer);
            removedTimer?.Dispose();
            _notificationQueue.Writer.TryWrite(new FileChangeNotification(workingDirectoryId, relativePath));
        }, null, dueTime, Timeout.InfiniteTimeSpan);

        if (_debounceTimers.TryGetValue(timerKey, out var previous) &&
            _debounceTimers.TryUpdate(timerKey, timer, previous))
        {
            previous.Dispose();
            return;
        }

        if (!_debounceTimers.TryAdd(timerKey, timer))
        {
            timer.Dispose();
        }
    }

    private void RemoveConnectionFromFile(FileWatchKey key, string connectionId)
    {
        if (_trackedFiles.TryGetValue(key, out var tracked))
        {
            var becameEmpty = false;
            lock (tracked.Gate)
            {
                tracked.Connections.Remove(connectionId);
                becameEmpty = tracked.Connections.Count == 0;
            }

            if (becameEmpty && _trackedFiles.TryRemove(key, out _))
            {
                CleanupWorkingDirectoryWatch(key.WorkingDirectoryId);
            }
        }

        if (_filesByConnection.TryGetValue(connectionId, out var connectionFiles))
        {
            lock (connectionFiles)
            {
                connectionFiles.Remove(key);
                if (connectionFiles.Count == 0)
                {
                    _filesByConnection.TryRemove(connectionId, out _);
                }
            }
        }
    }

    private void CleanupWorkingDirectoryWatch(Guid workingDirectoryId)
    {
        if (_trackedFiles.Keys.Any(item => item.WorkingDirectoryId == workingDirectoryId))
        {
            return;
        }

        if (_directoryWatches.TryRemove(workingDirectoryId, out var removed))
        {
            removed.Dispose();
        }
    }

    private void HandleWatcherError(Guid workingDirectoryId, ErrorEventArgs eventArgs)
    {
        if (!_directoryWatches.TryRemove(workingDirectoryId, out var directoryWatch))
        {
            return;
        }

        directoryWatch.Dispose();
        logger.LogWarning(
            eventArgs.GetException(),
            "Workspace file watcher failed for working directory {WorkingDirectoryId}",
            workingDirectoryId);
    }

    private sealed record FileWatchKey(Guid WorkingDirectoryId, string FileKey);

    private sealed record FileChangeNotification(Guid WorkingDirectoryId, string RelativePath);

    private sealed class TrackedFile(
        Guid workingDirectoryId,
        string fileKey,
        string normalizedRelativePath,
        string rootCanonical)
    {
        public Guid WorkingDirectoryId { get; } = workingDirectoryId;
        public string FileKey { get; } = fileKey;
        public string NormalizedRelativePath { get; } = normalizedRelativePath;
        public string RootCanonical { get; } = rootCanonical;
        public object Gate { get; } = new();
        public HashSet<string> Connections { get; } = new(StringComparer.Ordinal);
    }

    private sealed class WorkingDirectoryWatch : IDisposable
    {
        private readonly FileSystemWatcher _watcher;

        public WorkingDirectoryWatch(
            Guid workingDirectoryId,
            string rootCanonical,
            Action<string> onPathChanged,
            Action<string, string> onRenamed,
            Action<ErrorEventArgs> onError)
        {
            _ = workingDirectoryId;
            _ = rootCanonical;
            _watcher = new FileSystemWatcher(rootCanonical)
            {
                IncludeSubdirectories = true,
                InternalBufferSize = 64 * 1024,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _watcher.Changed += (_, args) => onPathChanged(args.FullPath);
            _watcher.Created += (_, args) => onPathChanged(args.FullPath);
            _watcher.Deleted += (_, args) => onPathChanged(args.FullPath);
            _watcher.Renamed += (_, args) => onRenamed(args.OldFullPath, args.FullPath);
            _watcher.Error += (_, args) => onError(args);
            _watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}