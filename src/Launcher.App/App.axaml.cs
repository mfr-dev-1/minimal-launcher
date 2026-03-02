using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Markup.Xaml;

namespace Launcher.App;

public partial class App : Avalonia.Application
{
    private MainWindow? _mainWindow;
    private Launcher.App.ViewModels.MainWindowViewModel_o? _mainWindowViewModel;
    private Launcher.App.Services.TrayIconService_c? _trayService;
    private Launcher.App.Services.GlobalHotkeyService_c? _hotkeyService;
    private Action? _toggleLauncherAction;
    private bool _runtimeReady;
    private bool _runtimeFailed;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var baseDir = AppContext.BaseDirectory;
        var settingsStore = new Launcher.Infrastructure.Storage.PortableSettingsStore_c(baseDir);
        var indexStore = new Launcher.Infrastructure.Storage.PortableIndexStore_c(baseDir);
        var scanner = new Launcher.Infrastructure.Indexing.FileProjectScanner_c();
        var indexer = new Launcher.Infrastructure.Indexing.FileProjectIndexer_o(scanner, indexStore);
        var toolWorkflow = new Launcher.Infrastructure.Tools.ToolDetectionWorkflow_o();
        var launcher = new Launcher.Infrastructure.Launch.ProjectLauncher_c();
        var terminal = new Launcher.Infrastructure.Launch.ShellTerminal_c();
        var aiChatCli = new Launcher.Infrastructure.Launch.AiChatCli_c();

        var settingsStorePort = new Launcher.Infrastructure.ApplicationPorts.PortableSettingsStorePort_c(settingsStore);
        var indexPort = new Launcher.Infrastructure.ApplicationPorts.FileProjectIndexPort_c(indexer);
        var toolPort = new Launcher.Infrastructure.ApplicationPorts.ToolDetectionPort_c(toolWorkflow);
        var launchPort = new Launcher.Infrastructure.ApplicationPorts.ProjectLaunchPort_c(launcher);
        var terminalPort = new Launcher.Infrastructure.ApplicationPorts.TerminalCommandPort_c(terminal);
        var aiChatPort = new Launcher.Infrastructure.ApplicationPorts.AiChatPort_c(aiChatCli);
        var application = new Launcher.Application.Runtime.LauncherApplication_o(
            settingsStorePort,
            indexPort,
            toolPort,
            launchPort,
            terminalPort,
            aiChatPort);

        _mainWindowViewModel = new Launcher.App.ViewModels.MainWindowViewModel_o(application);
        _mainWindow = new MainWindow(_mainWindowViewModel);
        _mainWindow.HotkeyUpdated += OnMainWindowHotkeyUpdated_c;

        desktop.MainWindow = _mainWindow;

        _trayService = new Launcher.App.Services.TrayIconService_c();
        _toggleLauncherAction = ToggleLauncherWhenReady_c;
        _trayService.Initialize(ToggleLauncherWhenReady_c, ShutdownApp_c);

        _hotkeyService = new Launcher.App.Services.GlobalHotkeyService_c();
        _mainWindow.Opened += (_, _) =>
        {
            RegisterHotkey_c(settingsStore.LoadOrCreate().Hotkeys.Toggle); 
        };

        _ = InitializeRuntimeAsync_c();
        desktop.Exit += OnDesktopExit_c;

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeRuntimeAsync_c()
    {
        if (_mainWindow is null)
        {
            return;
        }

        try
        {
            await _mainWindow.InitializeAsync(CancellationToken.None);
            _runtimeReady = true;
            if (_mainWindowViewModel?.IsOnboardingVisible == true)
            {
                _mainWindow.ShowLauncher();
            }
        }
        catch (Exception ex)
        {
            _runtimeFailed = true;
            _trayService?.ShowMessage("Launcher", $"Startup failed: {ex.Message}");
        }
    }

    private void OnDesktopExit_c(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_mainWindow is not null)
        {
            _mainWindow.HotkeyUpdated -= OnMainWindowHotkeyUpdated_c;
        }

        _hotkeyService?.Dispose();
        _trayService?.Dispose();
        _mainWindowViewModel?.Dispose();
    }

    private void ShutdownApp_c()
    {
        _mainWindow?.RequestApplicationExit();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void OnMainWindowHotkeyUpdated_c(object? sender, string hotkeyText)
    {
        RegisterHotkey_c(hotkeyText);
    }

    private void RegisterHotkey_c(string hotkeyText)
    {
        if (_mainWindow is null || _hotkeyService is null || _toggleLauncherAction is null)
        {
            return;
        }

        var handle = _mainWindow.TryGetPlatformHandle();
        if (handle is null || handle.Handle == IntPtr.Zero)
        {
            return;
        }

        var registered = _hotkeyService.Register(_mainWindow, hotkeyText, _toggleLauncherAction);
        if (!registered)
        {
            _trayService?.ShowMessage("Launcher", "Hotkey registration failed. Update launcher.settings.json hotkey value.");
        }
    }

    private void ToggleLauncherWhenReady_c()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (!_runtimeReady)
        {
            _trayService?.ShowMessage(
                "Launcher",
                _runtimeFailed
                    ? "Launcher startup failed. Restart after fixing settings."
                    : "Launcher is still starting. Please wait.");
            return;
        }

        _mainWindow.ToggleLauncher();
    }
}
