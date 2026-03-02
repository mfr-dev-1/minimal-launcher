using Launcher.Application.Ports;

namespace Launcher.Application.Tests;

public sealed class LauncherApplicationTests
{
    [Fact]
    public async Task Query_MetaPrefix_ReturnsMetaSuggestions()
    {
        var fixture = new AppFixture_c();
        using var app = fixture.CreateApplication();
        await app.InitializeAsync(CancellationToken.None);

        var projection = app.Query(">> onb");

        Assert.True(projection.IsMetaMode);
        Assert.Contains(projection.MetaSuggestions, x => string.Equals(x.Command, ">> onboarding", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecutePowerCommand_OnboardingMeta_RequestsOnboarding()
    {
        var fixture = new AppFixture_c();
        using var app = fixture.CreateApplication();
        await app.InitializeAsync(CancellationToken.None);

        var result = app.ExecutePowerCommand(">> onboarding");

        Assert.True(result.IsSuccess);
        Assert.True(result.RequestOpenOnboarding);
    }

    [Fact]
    public async Task ExecutePowerCommand_SettingsMeta_RequestsSettings()
    {
        var fixture = new AppFixture_c();
        using var app = fixture.CreateApplication();
        await app.InitializeAsync(CancellationToken.None);

        var result = app.ExecutePowerCommand(">> settings");

        Assert.True(result.IsSuccess);
        Assert.True(result.RequestOpenSettings);
    }

    [Fact]
    public async Task ExecutePowerCommand_ConfigMeta_Fails()
    {
        var fixture = new AppFixture_c();
        using var app = fixture.CreateApplication();
        await app.InitializeAsync(CancellationToken.None);

        var result = app.ExecutePowerCommand(">> config");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApplyPendingOnboardingDefaults_PendingState_DefaultsAndTransitions()
    {
        var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
        settings.Hotkeys.Toggle = string.Empty;
        settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Pending;

        var fixture = new AppFixture_c(settings);
        using var app = fixture.CreateApplication();
        await app.InitializeAsync(CancellationToken.None);

        var result = app.ApplyPendingOnboardingDefaults();

        Assert.True(result.IsSuccess);
        Assert.Equal(Launcher.Core.Models.OnboardingStates_c.Defaulted, app.Settings.Onboarding.State);
        Assert.False(string.IsNullOrWhiteSpace(app.Settings.Hotkeys.Toggle));
    }

    [Fact]
    public async Task CompleteOnboarding_MissingRoot_Fails()
    {
        var fixture = new AppFixture_c();
        using var app = fixture.CreateApplication();
        await app.InitializeAsync(CancellationToken.None);

        var result = app.CompleteOnboarding(["C:\\path\\that\\does\\not\\exist"], "Alt+Shift+Space", preferredToolId: null);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Launch_InvalidAlternativeIndex_Fails()
    {
        var fixture = new AppFixture_c();
        using var app = fixture.CreateApplication();
        await app.InitializeAsync(CancellationToken.None);

        var project = new Launcher.Core.Models.ProjectRecord_c
        {
            ProjectName = "launcher",
            ProjectPath = "C:\\launcher"
        };

        var searchResult = new Launcher.Core.Models.SearchResult_c
        {
            Project = project,
            SuggestedTool = fixture.AvailableTool,
            AlternativeTools = [],
            Score = 1
        };

        var result = app.Launch(searchResult, alternativeIndex: 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteTerminalCommandAsync_SinglePrefix_ExecutesAndPersistsOutput()
    {
        var fixture = new AppFixture_c();
        using var app = fixture.CreateApplication(
            terminalPort: new FakeTerminalCommandPort_c("ok"));
        await app.InitializeAsync(CancellationToken.None);

        var result = await app.ExecuteTerminalCommandAsync(">echo test", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("> echo test", app.GetTerminalOutputSnapshot());
        Assert.Contains("ok", app.GetTerminalOutputSnapshot());
    }

    [Fact]
    public async Task ExecuteTerminalCommandAsync_DoublePrefix_FailsDeterministically()
    {
        var fixture = new AppFixture_c();
        using var app = fixture.CreateApplication(
            terminalPort: new FakeTerminalCommandPort_c("unused"));
        await app.InitializeAsync(CancellationToken.None);

        var result = await app.ExecuteTerminalCommandAsync(">> refresh", CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    private sealed class AppFixture_c
    {
        private readonly FakeSettingsStorePort_c _settingsStorePort;
        private readonly FakeProjectIndexPort_c _projectIndexPort;
        private readonly FakeToolDetectionPort_c _toolDetectionPort;
        private readonly FakeProjectLaunchPort_c _projectLaunchPort;

        public Launcher.Core.Models.ToolRecord_c AvailableTool { get; } = new()
        {
            ToolId = Launcher.Core.Models.ToolIds_c.VsCode,
            DisplayName = "VS Code",
            ExecutablePath = "code.exe",
            IsAvailable = true
        };

        public AppFixture_c(Launcher.Core.Models.LauncherSettings_c? settings = null)
        {
            _settingsStorePort = new FakeSettingsStorePort_c(settings ?? Launcher.Core.Models.LauncherSettings_c.CreateDefault());
            _projectIndexPort = new FakeProjectIndexPort_c();
            _toolDetectionPort = new FakeToolDetectionPort_c([AvailableTool]);
            _projectLaunchPort = new FakeProjectLaunchPort_c();
        }

        public Launcher.Application.Runtime.LauncherApplication_o CreateApplication(
            ITerminalCommandPort_c? terminalPort = null,
            IAiChatPort_c? aiChatPort = null)
        {
            return new Launcher.Application.Runtime.LauncherApplication_o(
                _settingsStorePort,
                _projectIndexPort,
                _toolDetectionPort,
                _projectLaunchPort,
                terminalPort,
                aiChatPort);
        }
    }

    private sealed class FakeSettingsStorePort_c : ISettingsStorePort_c
    {
        private Launcher.Core.Models.LauncherSettings_c _settings;

        public FakeSettingsStorePort_c(Launcher.Core.Models.LauncherSettings_c settings)
        {
            _settings = settings;
        }

        public string SettingsPath => "C:\\launcher.settings.json";

        public Launcher.Core.Models.LauncherSettings_c LoadOrCreate()
        {
            return _settings;
        }

        public void Save(Launcher.Core.Models.LauncherSettings_c settings)
        {
            _settings = settings;
        }
    }

    private sealed class FakeProjectIndexPort_c : IProjectIndexPort_c
    {
        public event EventHandler<IReadOnlyList<Launcher.Core.Models.ProjectRecord_c>>? ProjectsUpdated;

        public Launcher.Core.Models.ProjectIndexSnapshot_c LoadCachedSnapshot()
        {
            return new Launcher.Core.Models.ProjectIndexSnapshot_c
            {
                Projects =
                [
                    new Launcher.Core.Models.ProjectRecord_c
                    {
                        ProjectName = "launcher",
                        ProjectPath = "C:\\launcher",
                        Landmarks = ["AGENTS.md"]
                    }
                ]
            };
        }

        public Task InitializeAsync(Launcher.Core.Models.LauncherSettings_c settings, CancellationToken cancellationToken)
        {
            var projects = LoadCachedSnapshot().Projects;
            ProjectsUpdated?.Invoke(this, projects);
            return Task.CompletedTask;
        }

        public Task RefreshAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void MarkProjectOpened(string projectPath)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeToolDetectionPort_c : IToolDetectionPort_c
    {
        private readonly List<Launcher.Core.Models.ToolRecord_c> _tools;

        public FakeToolDetectionPort_c(List<Launcher.Core.Models.ToolRecord_c> tools)
        {
            _tools = tools;
        }

        public List<Launcher.Core.Models.ToolRecord_c> Detect(Launcher.Core.Models.LauncherSettings_c settings)
        {
            return _tools.ToList();
        }
    }

    private sealed class FakeProjectLaunchPort_c : IProjectLaunchPort_c
    {
        public bool Launch(Launcher.Core.Models.ProjectRecord_c project, Launcher.Core.Models.ToolRecord_c tool)
        {
            return true;
        }

        public bool LaunchFile(Launcher.Core.Models.ProjectRecord_c project, Launcher.Core.Models.ToolRecord_c tool, string filePath)
        {
            return true;
        }

        public bool OpenPathInShell(string path)
        {
            return true;
        }

        public bool LaunchWindowsTerminal(string projectPath)
        {
            return true;
        }
    }

    private sealed class FakeTerminalCommandPort_c : ITerminalCommandPort_c
    {
        private readonly string _output;

        public FakeTerminalCommandPort_c(string output)
        {
            _output = output;
        }

        public Task<Launcher.Application.Models.TerminalCommandResult_c> ExecuteAsync(
            Launcher.Core.Models.TerminalSettings_c settings,
            string commandText,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Launcher.Application.Models.TerminalCommandResult_c.Success(commandText, _output, 0));
        }
    }
}
