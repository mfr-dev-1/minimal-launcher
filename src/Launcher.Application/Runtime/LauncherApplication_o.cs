using System.IO;
using Launcher.Application.Models;
using Launcher.Application.Ports;

namespace Launcher.Application.Runtime;

public sealed class LauncherApplication_o : IDisposable
{
    private static readonly MetaCommandSuggestion_c[] AllMetaCommands_c =
    [
        new() { Command = "! exit", Description = "Terminate launcher" },
        new() { Command = "! settings", Description = "Open launcher settings page" },
        new() { Command = "! refresh", Description = "Refresh tool detection and index" },
        new() { Command = "! index", Description = "Refresh index only" },
        new() { Command = "! onboarding", Description = "Open onboarding again" }
    ];

    private readonly ISettingsStorePort_c _settingsStorePort;
    private readonly IProjectIndexPort_c _projectIndexPort;
    private readonly IToolDetectionPort_c _toolDetectionPort;
    private readonly IProjectLaunchPort_c _projectLaunchPort;
    private readonly ITerminalCommandPort_c _terminalCommandPort;
    private readonly IAiChatPort_c _aiChatPort;

    private readonly Launcher.Core.Workflows.SearchProjects_o _searchWorkflow = new();
    private readonly Launcher.Core.Workflows.PowerModeCommandParser_c _commandParser = new();

    private readonly object _gate = new();
    private List<Launcher.Core.Models.ProjectRecord_c> _projects = [];
    private List<Launcher.Core.Models.ToolRecord_c> _tools = [];
    private List<string> _terminalOutput = [];

    public event EventHandler? IndexUpdated;

    public Launcher.Core.Models.LauncherSettings_c Settings { get; private set; } = Launcher.Core.Models.LauncherSettings_c.CreateDefault();

    public IReadOnlyList<Launcher.Core.Models.ProjectRecord_c> CurrentProjects
    {
        get
        {
            lock (_gate)
            {
                return _projects.ToList();
            }
        }
    }

    public IReadOnlyList<Launcher.Core.Models.ToolRecord_c> CurrentTools
    {
        get
        {
            lock (_gate)
            {
                return _tools.ToList();
            }
        }
    }

    public LauncherApplication_o(
        ISettingsStorePort_c settingsStorePort,
        IProjectIndexPort_c projectIndexPort,
        IToolDetectionPort_c toolDetectionPort,
        IProjectLaunchPort_c projectLaunchPort,
        ITerminalCommandPort_c? terminalCommandPort = null,
        IAiChatPort_c? aiChatPort = null)
    {
        _settingsStorePort = settingsStorePort;
        _projectIndexPort = projectIndexPort;
        _toolDetectionPort = toolDetectionPort;
        _projectLaunchPort = projectLaunchPort;
        _terminalCommandPort = terminalCommandPort ?? new NullTerminalCommandPort_c();
        _aiChatPort = aiChatPort ?? new NullAiChatPort_c();

        _projectIndexPort.ProjectsUpdated += OnProjectsUpdated_c;
    }

    public async Task<StartupState_c> InitializeAsync(CancellationToken cancellationToken)
    {
        Settings = _settingsStorePort.LoadOrCreate();
        var detectedTools = _toolDetectionPort.Detect(Settings);
        var cached = _projectIndexPort.LoadCachedSnapshot();

        lock (_gate)
        {
            _tools = detectedTools;
            _projects = cached.Projects;
        }

        await _projectIndexPort.InitializeAsync(Settings, cancellationToken);

        return new StartupState_c
        {
            Settings = Settings,
            CachedProjectCount = cached.Projects.Count,
            DetectedToolCount = detectedTools.Count(x => x.IsAvailable),
            IsOnboardingPending = IsOnboardingPending()
        };
    }

    public QueryProjection_c Query(string rawInput)
    {
        var safeInput = rawInput ?? string.Empty;
        if (IsMetaQuery_c(safeInput))
        {
            return new QueryProjection_c
            {
                IsMetaMode = true,
                MetaSuggestions = BuildMetaSuggestions_c(safeInput)
            };
        }

        lock (_gate)
        {
            return new QueryProjection_c
            {
                IsMetaMode = false,
                SearchResults = _searchWorkflow.Execute(safeInput, _projects, _tools, Settings)
            };
        }
    }

    public IReadOnlyList<string> BuildSuggestedOnboardingRoots()
    {
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var suggestions = new List<string>();

        if (!string.IsNullOrWhiteSpace(userHome))
        {
            suggestions.Add(userHome);
            suggestions.Add(Path.Combine(userHome, "Projects"));
            suggestions.Add(Path.Combine(userHome, "source", "repos"));
        }

        return suggestions
            .Where(x => !string.IsNullOrWhiteSpace(x) && Directory.Exists(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<Launcher.Core.Models.ToolRecord_c>> RefreshDetectedToolsAsync(CancellationToken cancellationToken)
    {
        var detectedTools = await Task.Run(() => _toolDetectionPort.Detect(Settings), cancellationToken);
        lock (_gate)
        {
            _tools = detectedTools;
        }

        IndexUpdated?.Invoke(this, EventArgs.Empty);
        return detectedTools;
    }

    public bool IsOnboardingPending()
    {
        return string.Equals(
            Settings.Onboarding.State,
            Launcher.Core.Models.OnboardingStates_c.Pending,
            StringComparison.OrdinalIgnoreCase);
    }

    public OnboardingTransitionResult_c CompleteOnboarding(
        IReadOnlyList<string> rootPaths,
        string hotkeyText,
        string? preferredToolId)
    {
        var normalizedRoots = new List<string>();
        foreach (var rootPath in rootPaths.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            try
            {
                normalizedRoots.Add(Path.GetFullPath(rootPath));
            }
            catch
            {
                // Ignore malformed paths and let validation fail.
            }
        }

        normalizedRoots = normalizedRoots
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedRoots.Count == 0)
        {
            return OnboardingTransitionResult_c.Fail("Onboarding requires at least one root path.");
        }

        if (normalizedRoots.Any(x => !Directory.Exists(x)))
        {
            return OnboardingTransitionResult_c.Fail("Onboarding root path must exist.");
        }

        if (string.IsNullOrWhiteSpace(hotkeyText))
        {
            return OnboardingTransitionResult_c.Fail("Onboarding requires a valid global hotkey.");
        }

        Settings.Roots = normalizedRoots
            .Select(x => new Launcher.Core.Models.RootPathSetting_c { Path = x, Enabled = true })
            .ToList();
        Settings.Hotkeys.Toggle = hotkeyText.Trim();

        if (!string.IsNullOrWhiteSpace(preferredToolId))
        {
            Settings.Defaults.FallbackToolId = preferredToolId;
            Settings.Defaults.ToolPriorityOrder = BuildToolPriorityWithPreferred_c(
                preferredToolId,
                Settings.Defaults.ToolPriorityOrder);
        }

        Settings.Onboarding.Version = Launcher.Core.Models.OnboardingSettings_c.CurrentVersion_c;
        Settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Completed;
        Settings.Onboarding.LastSeenUtc = DateTime.UtcNow;

        SaveSettingsQuietly_c();
        QueueFullRefresh_c();
        return OnboardingTransitionResult_c.Success("Onboarding complete.");
    }

    public OnboardingTransitionResult_c SkipOnboarding()
    {
        Settings.Onboarding.Version = Launcher.Core.Models.OnboardingSettings_c.CurrentVersion_c;
        Settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Skipped;
        Settings.Onboarding.LastSeenUtc = DateTime.UtcNow;
        SaveSettingsQuietly_c();
        return OnboardingTransitionResult_c.Success("Onboarding skipped.");
    }

    public OnboardingTransitionResult_c ApplyPendingOnboardingDefaults()
    {
        if (!IsOnboardingPending())
        {
            return OnboardingTransitionResult_c.Success("Onboarding defaulting not required.");
        }

        var suggestedRoots = BuildSuggestedOnboardingRoots();
        var selectedRoot = suggestedRoots.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(selectedRoot))
        {
            Settings.Roots =
            [
                new Launcher.Core.Models.RootPathSetting_c
                {
                    Path = selectedRoot,
                    Enabled = true
                }
            ];
        }

        if (string.IsNullOrWhiteSpace(Settings.Hotkeys.Toggle))
        {
            Settings.Hotkeys.Toggle = "Alt+Shift+Space";
        }

        var preferredToolId = CurrentTools
            .Where(x => x.IsAvailable)
            .Select(x => x.ToolId)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(preferredToolId))
        {
            Settings.Defaults.FallbackToolId = preferredToolId;
            Settings.Defaults.ToolPriorityOrder = BuildToolPriorityWithPreferred_c(
                preferredToolId,
                Settings.Defaults.ToolPriorityOrder);
        }

        Settings.Onboarding.Version = Launcher.Core.Models.OnboardingSettings_c.CurrentVersion_c;
        Settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Defaulted;
        Settings.Onboarding.LastSeenUtc = DateTime.UtcNow;

        SaveSettingsQuietly_c();
        QueueFullRefresh_c();
        return OnboardingTransitionResult_c.Success("Onboarding defaulted and launcher is ready.");
    }

    public void MarkOnboardingSeen()
    {
        Settings.Onboarding.LastSeenUtc = DateTime.UtcNow;
        SaveSettingsQuietly_c();
    }

    public OnboardingTransitionResult_c PersistSettings()
    {
        try
        {
            _settingsStorePort.Save(Settings);
            return OnboardingTransitionResult_c.Success("Settings saved.");
        }
        catch (Exception ex)
        {
            return OnboardingTransitionResult_c.Fail($"Settings save failed: {ex.Message}");
        }
    }

    public LaunchExecutionResult_c Launch(Launcher.Core.Models.SearchResult_c selectedResult, int? alternativeIndex)
    {
        Launcher.Core.Models.ToolRecord_c tool = selectedResult.SuggestedTool;
        if (alternativeIndex.HasValue)
        {
            if (alternativeIndex.Value < 0 || alternativeIndex.Value >= selectedResult.AlternativeTools.Count)
            {
                return LaunchExecutionResult_c.Fail("Alternative tool slot not available.");
            }

            tool = selectedResult.AlternativeTools[alternativeIndex.Value];
        }

        var launched = _projectLaunchPort.Launch(selectedResult.Project, tool);
        if (!launched)
        {
            return LaunchExecutionResult_c.Fail("Launch failed. Tool is unavailable.");
        }

        _projectIndexPort.MarkProjectOpened(selectedResult.Project.ProjectPath);
        lock (_gate)
        {
            var project = _projects.FirstOrDefault(x =>
                string.Equals(x.ProjectPath, selectedResult.Project.ProjectPath, StringComparison.OrdinalIgnoreCase));
            if (project is not null)
            {
                project.LastOpenedUtc = DateTime.UtcNow;
            }

            RecordLastUsedToolForProject_c(selectedResult.Project.ProjectPath, tool.ToolId);
        }

        SaveSettingsQuietly_c();
        return LaunchExecutionResult_c.Success($"Opened {selectedResult.Project.ProjectName} with {tool.DisplayName}.");
    }

    public LaunchExecutionResult_c LaunchFile(
        Launcher.Core.Models.ProjectRecord_c project,
        Launcher.Core.Models.ToolRecord_c tool,
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return LaunchExecutionResult_c.Fail("File path is empty.");
        }

        var launched = _projectLaunchPort.LaunchFile(project, tool, filePath);
        if (!launched)
        {
            return LaunchExecutionResult_c.Fail("File launch failed. Tool is unavailable.");
        }

        _projectIndexPort.MarkProjectOpened(project.ProjectPath);
        lock (_gate)
        {
            var existingProject = _projects.FirstOrDefault(x =>
                string.Equals(x.ProjectPath, project.ProjectPath, StringComparison.OrdinalIgnoreCase));
            if (existingProject is not null)
            {
                existingProject.LastOpenedUtc = DateTime.UtcNow;
            }

            RecordLastUsedToolForProject_c(project.ProjectPath, tool.ToolId);
        }

        SaveSettingsQuietly_c();
        return LaunchExecutionResult_c.Success($"Opened {Path.GetFileName(filePath)} with {tool.DisplayName}.");
    }

    public async Task<TerminalCommandResult_c> ExecuteTerminalCommandAsync(string rawInput, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return TerminalCommandResult_c.Fail(string.Empty, "Terminal command is empty.");
        }

        var trimmed = rawInput.Trim();
        if (!trimmed.StartsWith(">", StringComparison.Ordinal) || trimmed.StartsWith(">>", StringComparison.Ordinal))
        {
            return TerminalCommandResult_c.Fail(trimmed, "Input is not terminal mode command.");
        }

        var commandText = trimmed[1..].Trim();
        if (commandText.Length == 0)
        {
            return TerminalCommandResult_c.Fail(commandText, "Terminal command is empty.");
        }

        var terminalResult = await _terminalCommandPort.ExecuteAsync(Settings.Terminal, commandText, cancellationToken);
        lock (_gate)
        {
            _terminalOutput.Add($"> {commandText}");
            if (terminalResult.IsSuccess)
            {
                var hasStructuredOutput = !string.IsNullOrWhiteSpace(terminalResult.StandardOutputText) ||
                                          !string.IsNullOrWhiteSpace(terminalResult.StandardErrorText);
                if (hasStructuredOutput)
                {
                    AppendTerminalLines_c(_terminalOutput, terminalResult.StandardOutputText, isError: false);
                    AppendTerminalLines_c(_terminalOutput, terminalResult.StandardErrorText, isError: true);
                    if (string.IsNullOrWhiteSpace(terminalResult.StandardOutputText) &&
                        string.IsNullOrWhiteSpace(terminalResult.StandardErrorText))
                    {
                        _terminalOutput.Add("(no output)");
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(terminalResult.OutputText))
                    {
                        _terminalOutput.Add("(no output)");
                    }
                    else
                    {
                        foreach (var line in terminalResult.OutputText
                                     .Split(Environment.NewLine, StringSplitOptions.None))
                        {
                            _terminalOutput.Add(line);
                        }
                    }
                }
            }
            else
            {
                _terminalOutput.Add($"(error) {terminalResult.ErrorMessage}");
            }
        }

        return terminalResult;
    }

    public IReadOnlyList<string> GetTerminalOutputSnapshot()
    {
        lock (_gate)
        {
            return _terminalOutput.ToList();
        }
    }

    public void RestoreTerminalOutput(IReadOnlyList<string> lines)
    {
        lock (_gate)
        {
            _terminalOutput = [..lines];
        }
    }

    public void ClearTerminalOutput()
    {
        lock (_gate)
        {
            _terminalOutput = [];
        }
    }

    private static void AppendTerminalLines_c(List<string> sink, string rawText, bool isError)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return;
        }

        foreach (var line in rawText.Split(Environment.NewLine, StringSplitOptions.None))
        {
            if (isError)
            {
                sink.Add($"(stderr) {line}");
            }
            else
            {
                sink.Add(line);
            }
        }
    }

    public async Task<AiChatResult_c> ExecuteAiChatAsync(string prompt, CancellationToken cancellationToken)
    {
        return await _aiChatPort.ExecuteAsync(Settings.AiChat, prompt, cancellationToken);
    }

    public bool IsTextEditorTool(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            return false;
        }

        return TextEditorTools_c.Contains(toolId);
    }

    public CommandExecutionResult_c ExecutePowerCommand(string rawInput)
    {
        var parsed = _commandParser.Parse(rawInput);
        if (!parsed.IsCommand)
        {
            return CommandExecutionResult_c.Fail("Not a command.");
        }

        if (!parsed.IsValid)
        {
            return CommandExecutionResult_c.Fail(parsed.Error);
        }

        if (parsed.IsWindowsTerminal)
        {
            Launcher.Core.Models.ProjectRecord_c? project;
            lock (_gate)
            {
                project = _projects.FirstOrDefault(x => string.Equals(x.ProjectName, parsed.ProjectHint, StringComparison.OrdinalIgnoreCase))
                    ?? _projects.FirstOrDefault(x => x.ProjectName.Contains(parsed.ProjectHint, StringComparison.OrdinalIgnoreCase));
            }

            if (project is null)
            {
                return CommandExecutionResult_c.Fail("Project not found for wterm command.");
            }

            var launched = _projectLaunchPort.LaunchWindowsTerminal(project.ProjectPath);
            return launched
                ? CommandExecutionResult_c.Success($"Opened Windows Terminal in {project.ProjectName}.")
                : CommandExecutionResult_c.Fail("Failed to open Windows Terminal.");
        }

        if (parsed.IsMetaExit)
        {
            return CommandExecutionResult_c.Success("Meta exit: terminating launcher.", requestAppExit: true);
        }

        if (parsed.IsMetaConfig)
        {
            return CommandExecutionResult_c.Success(
                "Meta settings: opening settings page.",
                requestOpenSettings: true);
        }

        if (parsed.IsMetaIndex)
        {
            QueueIndexRefresh_c();
            return CommandExecutionResult_c.Success("Meta index: index refresh started.");
        }

        if (parsed.IsMetaRefresh)
        {
            QueueFullRefresh_c();
            return CommandExecutionResult_c.Success("Meta refresh: tool+index refresh started.");
        }

        if (parsed.IsMetaOnboarding)
        {
            return CommandExecutionResult_c.Success("Opening onboarding.", requestOpenOnboarding: true);
        }

        return CommandExecutionResult_c.Fail("Not implemented command.");
    }

    public void Dispose()
    {
        _projectIndexPort.ProjectsUpdated -= OnProjectsUpdated_c;
        _projectIndexPort.Dispose();
    }

    private void OnProjectsUpdated_c(object? sender, IReadOnlyList<Launcher.Core.Models.ProjectRecord_c> projects)
    {
        lock (_gate)
        {
            _projects = projects.ToList();
        }

        IndexUpdated?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsMetaQuery_c(string query)
    {
        return query.TrimStart().StartsWith("!", StringComparison.Ordinal);
    }

    private static IReadOnlyList<MetaCommandSuggestion_c> BuildMetaSuggestions_c(string rawQuery)
    {
        var trimmed = rawQuery.Trim();
        var filter = trimmed.Length >= 2 ? trimmed[1..].Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(filter))
        {
            return AllMetaCommands_c.ToList();
        }

        return AllMetaCommands_c
            .Where(x => x.Command.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        x.Description.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void QueueIndexRefresh_c()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _projectIndexPort.RefreshAsync(CancellationToken.None);
            }
            catch
            {
                // Keep app running on refresh failure.
            }
        });
    }

    private void QueueFullRefresh_c()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var refreshedTools = _toolDetectionPort.Detect(Settings);
                lock (_gate)
                {
                    _tools = refreshedTools;
                }

                IndexUpdated?.Invoke(this, EventArgs.Empty);
                await _projectIndexPort.RefreshAsync(CancellationToken.None);
            }
            catch
            {
                // Keep app running on refresh failure.
            }
        });
    }

    private void RecordLastUsedToolForProject_c(string projectPath, string toolId)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || string.IsNullOrWhiteSpace(toolId))
        {
            return;
        }

        var existing = Settings.LastUsedProjectTools
            .FirstOrDefault(x => string.Equals(x.ProjectPath, projectPath, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            Settings.LastUsedProjectTools.Add(new Launcher.Core.Models.ProjectLastUsedTool_c
            {
                ProjectPath = projectPath,
                ToolId = toolId,
                LastUsedUtc = DateTime.UtcNow
            });
            return;
        }

        existing.ToolId = toolId;
        existing.LastUsedUtc = DateTime.UtcNow;
    }

    private void SaveSettingsQuietly_c()
    {
        try
        {
            _settingsStorePort.Save(Settings);
        }
        catch
        {
            // Keep launcher running when settings write fails.
        }
    }

    private static List<string> BuildToolPriorityWithPreferred_c(string preferredToolId, IReadOnlyList<string> currentOrder)
    {
        var merged = new List<string>
        {
            preferredToolId
        };

        merged.AddRange(currentOrder.Where(x => !string.IsNullOrWhiteSpace(x)));
        merged.AddRange(Launcher.Core.Models.DefaultSettings_c.BuildDefaultToolPriorityOrder_c());
        return merged
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static readonly HashSet<string> TextEditorTools_c = new(StringComparer.OrdinalIgnoreCase)
    {
        Launcher.Core.Models.ToolIds_c.SublimeText,
        Launcher.Core.Models.ToolIds_c.NotepadPlusPlus,
        Launcher.Core.Models.ToolIds_c.Vim,
        Launcher.Core.Models.ToolIds_c.NeoVim
    };

    private sealed class NullTerminalCommandPort_c : ITerminalCommandPort_c
    {
        public Task<TerminalCommandResult_c> ExecuteAsync(
            Launcher.Core.Models.TerminalSettings_c settings,
            string commandText,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(TerminalCommandResult_c.Fail(commandText, "Terminal backend not configured."));
        }
    }

    private sealed class NullAiChatPort_c : IAiChatPort_c
    {
        public Task<AiChatResult_c> ExecuteAsync(
            Launcher.Core.Models.AiChatSettings_c settings,
            string prompt,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AiChatResult_c.Fail("AI chat backend not configured."));
        }
    }
}
