namespace Launcher.Application.Ports;

public interface ISettingsStorePort_c
{
    string SettingsPath { get; }

    Launcher.Core.Models.LauncherSettings_c LoadOrCreate();

    void Save(Launcher.Core.Models.LauncherSettings_c settings);
}

public interface IProjectIndexPort_c : IDisposable
{
    event EventHandler<IReadOnlyList<Launcher.Core.Models.ProjectRecord_c>>? ProjectsUpdated;

    Launcher.Core.Models.ProjectIndexSnapshot_c LoadCachedSnapshot();

    Task InitializeAsync(Launcher.Core.Models.LauncherSettings_c settings, CancellationToken cancellationToken);

    Task RefreshAsync(CancellationToken cancellationToken);

    void MarkProjectOpened(string projectPath);
}

public interface IToolDetectionPort_c
{
    List<Launcher.Core.Models.ToolRecord_c> Detect(Launcher.Core.Models.LauncherSettings_c settings);
}

public interface IProjectLaunchPort_c
{
    bool Launch(Launcher.Core.Models.ProjectRecord_c project, Launcher.Core.Models.ToolRecord_c tool);

    bool LaunchFile(Launcher.Core.Models.ProjectRecord_c project, Launcher.Core.Models.ToolRecord_c tool, string filePath);

    bool OpenPathInShell(string path);

    bool LaunchWindowsTerminal(string projectPath);
}

public interface ITerminalCommandPort_c
{
    Task<Launcher.Application.Models.TerminalCommandResult_c> ExecuteAsync(
        Launcher.Core.Models.TerminalSettings_c settings,
        string commandText,
        CancellationToken cancellationToken);
}

public interface IAiChatPort_c
{
    Task<Launcher.Application.Models.AiChatResult_c> ExecuteAsync(
        Launcher.Core.Models.AiChatSettings_c settings,
        string prompt,
        CancellationToken cancellationToken);
}
