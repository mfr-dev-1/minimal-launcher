namespace Launcher.Infrastructure.ApplicationPorts;

public sealed class FileProjectIndexPort_c : Launcher.Application.Ports.IProjectIndexPort_c
{
    private readonly Launcher.Infrastructure.Indexing.FileProjectIndexer_o _innerIndexer;

    public event EventHandler<IReadOnlyList<Launcher.Core.Models.ProjectRecord_c>>? ProjectsUpdated;

    public FileProjectIndexPort_c(Launcher.Infrastructure.Indexing.FileProjectIndexer_o innerIndexer)
    {
        _innerIndexer = innerIndexer;
        _innerIndexer.ProjectsUpdated += OnInnerProjectsUpdated_c;
    }

    public Launcher.Core.Models.ProjectIndexSnapshot_c LoadCachedSnapshot()
    {
        return _innerIndexer.LoadCachedSnapshot();
    }

    public Task InitializeAsync(Launcher.Core.Models.LauncherSettings_c settings, CancellationToken cancellationToken)
    {
        return _innerIndexer.InitializeAsync(settings, cancellationToken);
    }

    public Task RefreshAsync(CancellationToken cancellationToken)
    {
        return _innerIndexer.RefreshAsync(cancellationToken);
    }

    public void MarkProjectOpened(string projectPath)
    {
        _innerIndexer.MarkProjectOpened(projectPath);
    }

    public void Dispose()
    {
        _innerIndexer.ProjectsUpdated -= OnInnerProjectsUpdated_c;
        _innerIndexer.Dispose();
    }

    private void OnInnerProjectsUpdated_c(object? sender, IReadOnlyList<Launcher.Core.Models.ProjectRecord_c> projects)
    {
        ProjectsUpdated?.Invoke(this, projects);
    }
}
