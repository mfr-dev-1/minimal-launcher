using Launcher.Core.Models;
using Launcher.Infrastructure.Storage;

namespace Launcher.Infrastructure.Indexing;

public sealed class FileProjectIndexer_o : IDisposable
{
    private readonly FileProjectScanner_c _scanner;
    private readonly PortableIndexStore_c _indexStore;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
    private readonly TimeSpan _watchDebounce = TimeSpan.FromMilliseconds(300);
    private readonly TimeSpan _reconcileInterval = TimeSpan.FromMinutes(15);

    private CancellationTokenSource? _refreshCts;
    private Timer? _watchDebounceTimer;
    private Timer? _reconcileTimer;
    private LauncherSettings_c? _settings;

    public event EventHandler<IReadOnlyList<ProjectRecord_c>>? ProjectsUpdated;

    public FileProjectIndexer_o(FileProjectScanner_c scanner, PortableIndexStore_c indexStore)
    {
        _scanner = scanner;
        _indexStore = indexStore;
    }

    public ProjectIndexSnapshot_c LoadCachedSnapshot()
    {
        return _indexStore.Load();
    }

    public Task InitializeAsync(LauncherSettings_c settings, CancellationToken cancellationToken)
    {
        _settings = settings;
        SetupWatchers(settings);
        _reconcileTimer = new Timer(_ => _ = RefreshAsync(CancellationToken.None), null, _reconcileInterval, _reconcileInterval);
        _ = RefreshAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public void MarkProjectOpened(string projectPath)
    {
        var snapshot = _indexStore.Load();
        var project = snapshot.Projects.FirstOrDefault(x => string.Equals(x.ProjectPath, projectPath, StringComparison.OrdinalIgnoreCase));
        if (project is null)
        {
            return;
        }

        project.LastOpenedUtc = DateTime.UtcNow;
        snapshot.LastUpdatedUtc = DateTime.UtcNow;
        _indexStore.Save(snapshot);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (_settings is null)
        {
            return;
        }

        await _refreshSemaphore.WaitAsync(cancellationToken);
        try
        {
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var projects = await Task.Run(
                () => _scanner.ScanProjects(_settings.Roots, _settings.Indexing, _refreshCts.Token),
                _refreshCts.Token);

            var cached = _indexStore.Load();
            foreach (var project in projects)
            {
                var existing = cached.Projects.FirstOrDefault(x => string.Equals(x.ProjectPath, project.ProjectPath, StringComparison.OrdinalIgnoreCase));
                if (existing?.LastOpenedUtc is not null)
                {
                    project.LastOpenedUtc = existing.LastOpenedUtc;
                }
            }

            var snapshot = new ProjectIndexSnapshot_c
            {
                LastUpdatedUtc = DateTime.UtcNow,
                Projects = projects
            };

            _indexStore.Save(snapshot);
            ProjectsUpdated?.Invoke(this, projects);
        }
        catch (OperationCanceledException)
        {
            // Ignore superseded refresh requests.
        }
        catch
        {
            // Keep app responsive when scanner encounters transient IO failures.
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

    private void SetupWatchers(LauncherSettings_c settings)
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _watchers.Clear();

        foreach (var root in settings.Roots.Where(x => x.Enabled && Directory.Exists(x.Path)))
        {
            var watcher = new FileSystemWatcher(root.Path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Changed += OnWatcherChanged;
            watcher.Created += OnWatcherChanged;
            watcher.Renamed += OnWatcherChanged;
            watcher.Deleted += OnWatcherChanged;
            watcher.Error += OnWatcherError;

            _watchers.Add(watcher);
        }
    }

    private void OnWatcherChanged(object sender, FileSystemEventArgs e)
    {
        _watchDebounceTimer ??= new Timer(_ => _ = RefreshAsync(CancellationToken.None), null, Timeout.Infinite, Timeout.Infinite);
        _watchDebounceTimer.Change(_watchDebounce, Timeout.InfiniteTimeSpan);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _ = RefreshAsync(CancellationToken.None);
    }

    public void Dispose()
    {
        _watchDebounceTimer?.Dispose();
        _reconcileTimer?.Dispose();
        _refreshCts?.Dispose();
        _refreshSemaphore.Dispose();

        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }
    }
}
