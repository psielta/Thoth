using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Infrastructure.FileSystem;

public sealed class LinkedDocumentWatcherService(
    IServiceScopeFactory scopeFactory,
    IOptions<LinkedDocumentOptions> options,
    ILogger<LinkedDocumentWatcherService> logger)
    : BackgroundService, ILinkedDocumentWatchCoordinator, IDisposable
{
    private readonly Channel<Guid> _syncQueue = Channel.CreateUnbounded<Guid>();
    private readonly ConcurrentDictionary<Guid, TrackedDocument> _trackedDocuments = new();
    private readonly ConcurrentDictionary<string, DirectoryWatch> _directoryWatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Timer> _debounceTimers = new(StringComparer.OrdinalIgnoreCase);

    public async Task StartTrackingAsync(Guid linkedDocumentId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var document = context.LinkedDocuments.FirstOrDefault(item => item.Id == linkedDocumentId);

        if (document is null || document.Status == LinkedDocumentStatus.Paused)
        {
            return;
        }

        var prompt = context.Prompts.FirstOrDefault(item => item.Id == document.PromptId);
        if (prompt is null)
        {
            return;
        }

        if (prompt.Status == PromptStatus.Archived)
        {
            var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
            document.Status = LinkedDocumentStatus.Paused;
            document.UpdatedAtUtc = dateTimeProvider.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            StopTracking(linkedDocumentId);
            return;
        }

        var directory = Path.GetDirectoryName(document.AbsolutePath);
        var fileName = Path.GetFileName(document.AbsolutePath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName) || !Directory.Exists(directory))
        {
            await MarkMissingAsync(
                new[] { linkedDocumentId },
                "Linked markdown directory was not found.",
                cancellationToken);
            return;
        }

        var directoryKey = LinkedDocumentPath.CreateKey(directory);
        var tracked = new TrackedDocument(
            document.Id,
            document.PromptId,
            prompt.WorkingDirectoryId,
            document.AbsolutePath,
            document.AbsolutePathKey,
            directoryKey,
            fileName);

        if (_trackedDocuments.TryGetValue(document.Id, out var previous) && previous.DirectoryKey != directoryKey)
        {
            RemoveFromDirectoryWatch(previous);
        }

        _trackedDocuments[document.Id] = tracked;
        var directoryWatch = _directoryWatches.GetOrAdd(
            directoryKey,
            _ => CreateDirectoryWatch(directory, directoryKey));
        directoryWatch.Add(fileName, document.Id);
    }

    public void StopTracking(Guid linkedDocumentId)
    {
        if (_trackedDocuments.TryRemove(linkedDocumentId, out var tracked))
        {
            RemoveFromDirectoryWatch(tracked);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RegisterExistingDocumentsAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Linked document watcher startup registration failed; reconciliation will retry.");
        }

        await Task.WhenAll(
            ProcessQueueAsync(stoppingToken),
            ReconcileLoopAsync(stoppingToken));
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _syncQueue.Writer.TryComplete();
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
        _trackedDocuments.Clear();
        base.Dispose();
    }

    private async Task RegisterExistingDocumentsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var documents = context.LinkedDocuments
            .Where(document => document.Status == LinkedDocumentStatus.Tracking ||
                               document.Status == LinkedDocumentStatus.Error ||
                               document.Status == LinkedDocumentStatus.Missing)
            .Select(document => document.Id)
            .ToList();

        foreach (var documentId in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await StartTrackingAsync(documentId, cancellationToken);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (var documentId in _syncQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ILinkedDocumentSyncService>();
                var notifier = scope.ServiceProvider.GetRequiredService<ILinkedDocumentNotifier>();
                var outcome = await syncService.SyncAsync(
                    documentId,
                    LinkedDocumentVersionSource.FileChanged,
                    stoppingToken);

                if (outcome.Document is null)
                {
                    StopTracking(documentId);
                    continue;
                }

                if (outcome.Document.Status == LinkedDocumentStatus.Tracking)
                {
                    await StartTrackingAsync(documentId, stoppingToken);
                }
                else
                {
                    StopTracking(documentId);
                }

                if ((outcome.Changed || outcome.StatusChanged) && outcome.PromptWorkingDirectoryId is { } workingDirectoryId)
                {
                    await notifier.LinkedDocumentUpdatedAsync(outcome.Document, workingDirectoryId, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to synchronize linked document {LinkedDocumentId}", documentId);
            }
        }
    }

    private async Task ReconcileLoopAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(options.Value.ReconcileSeconds, 5));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var documentIds = context.LinkedDocuments
                    .Where(document => document.Status == LinkedDocumentStatus.Tracking ||
                                       document.Status == LinkedDocumentStatus.Error ||
                                       document.Status == LinkedDocumentStatus.Missing)
                    .Select(document => document.Id)
                    .ToList();

                foreach (var documentId in documentIds)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await StartTrackingAsync(documentId, stoppingToken);
                    QueueSync(documentId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to reconcile linked document watchers");
            }
        }
    }

    private DirectoryWatch CreateDirectoryWatch(string directory, string directoryKey)
    {
        var watch = new DirectoryWatch(
            directory,
            path => SchedulePath(directoryKey, path),
            (oldPath, newPath) =>
            {
                SchedulePath(directoryKey, oldPath);
                SchedulePath(directoryKey, newPath);
            },
            exception => HandleWatcherError(directoryKey, exception));

        return watch;
    }

    private void SchedulePath(string directoryKey, string path)
    {
        if (!_directoryWatches.TryGetValue(directoryKey, out var directoryWatch))
        {
            return;
        }

        foreach (var documentId in directoryWatch.GetDocumentIds(Path.GetFileName(path)))
        {
            ScheduleSync(documentId);
        }
    }

    private void ScheduleSync(Guid documentId)
    {
        if (!_trackedDocuments.TryGetValue(documentId, out var tracked))
        {
            return;
        }

        var timerKey = tracked.AbsolutePathKey;
        var dueTime = TimeSpan.FromMilliseconds(Math.Max(options.Value.DebounceMilliseconds, 100));
        var timer = new Timer(_ =>
        {
            _debounceTimers.TryRemove(timerKey, out var removedTimer);
            removedTimer?.Dispose();
            QueueSync(documentId);
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

    private void QueueSync(Guid documentId)
    {
        _syncQueue.Writer.TryWrite(documentId);
    }

    private void HandleWatcherError(string directoryKey, ErrorEventArgs eventArgs)
    {
        if (!_directoryWatches.TryRemove(directoryKey, out var directoryWatch))
        {
            return;
        }

        var documentIds = directoryWatch.GetAllDocumentIds().ToList();
        directoryWatch.Dispose();

        foreach (var documentId in documentIds)
        {
            _trackedDocuments.TryRemove(documentId, out _);
        }

        _ = MarkMissingAsync(
            documentIds,
            eventArgs.GetException().Message,
            CancellationToken.None);
    }

    private async Task MarkMissingAsync(
        IEnumerable<Guid> documentIds,
        string error,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<ILinkedDocumentNotifier>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var now = dateTimeProvider.UtcNow;

        foreach (var documentId in documentIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var document = context.LinkedDocuments.FirstOrDefault(item => item.Id == documentId);
            if (document is null || document.Status == LinkedDocumentStatus.Paused)
            {
                continue;
            }

            var prompt = context.Prompts.FirstOrDefault(item => item.Id == document.PromptId);
            if (prompt is null)
            {
                continue;
            }

            if (document.Status != LinkedDocumentStatus.Missing || document.LastError != error)
            {
                document.Status = LinkedDocumentStatus.Missing;
                document.LastError = error;
                document.UpdatedAtUtc = now;
                await context.SaveChangesAsync(cancellationToken);
                await notifier.LinkedDocumentUpdatedAsync(document.ToDto(), prompt.WorkingDirectoryId, cancellationToken);
            }
        }
    }

    private void RemoveFromDirectoryWatch(TrackedDocument tracked)
    {
        if (!_directoryWatches.TryGetValue(tracked.DirectoryKey, out var directoryWatch))
        {
            return;
        }

        directoryWatch.Remove(tracked.FileName, tracked.Id);
        if (directoryWatch.IsEmpty)
        {
            if (_directoryWatches.TryRemove(tracked.DirectoryKey, out var removed))
            {
                removed.Dispose();
            }
        }
    }

    private sealed record TrackedDocument(
        Guid Id,
        Guid PromptId,
        Guid PromptWorkingDirectoryId,
        string AbsolutePath,
        string AbsolutePathKey,
        string DirectoryKey,
        string FileName);

    private sealed class DirectoryWatch : IDisposable
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, HashSet<Guid>> _documentIdsByFileName = new(StringComparer.OrdinalIgnoreCase);
        private readonly FileSystemWatcher _watcher;

        public DirectoryWatch(
            string directory,
            Action<string> onPathChanged,
            Action<string, string> onRenamed,
            Action<ErrorEventArgs> onError)
        {
            _watcher = new FileSystemWatcher(directory)
            {
                IncludeSubdirectories = false,
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

        public bool IsEmpty
        {
            get
            {
                lock (_gate)
                {
                    return _documentIdsByFileName.Count == 0;
                }
            }
        }

        public void Add(string fileName, Guid documentId)
        {
            lock (_gate)
            {
                if (!_documentIdsByFileName.TryGetValue(fileName, out var documentIds))
                {
                    documentIds = new HashSet<Guid>();
                    _documentIdsByFileName[fileName] = documentIds;
                }

                documentIds.Add(documentId);
            }
        }

        public void Remove(string fileName, Guid documentId)
        {
            lock (_gate)
            {
                if (!_documentIdsByFileName.TryGetValue(fileName, out var documentIds))
                {
                    return;
                }

                documentIds.Remove(documentId);
                if (documentIds.Count == 0)
                {
                    _documentIdsByFileName.Remove(fileName);
                }
            }
        }

        public IReadOnlyList<Guid> GetDocumentIds(string fileName)
        {
            lock (_gate)
            {
                return _documentIdsByFileName.TryGetValue(fileName, out var documentIds)
                    ? documentIds.ToList()
                    : Array.Empty<Guid>();
            }
        }

        public IReadOnlyList<Guid> GetAllDocumentIds()
        {
            lock (_gate)
            {
                return _documentIdsByFileName.Values.SelectMany(item => item).Distinct().ToList();
            }
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}
