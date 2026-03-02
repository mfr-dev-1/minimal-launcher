using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Launcher.App;

public partial class MainWindow : Window
{
    private readonly Launcher.App.ViewModels.MainWindowViewModel_o _viewModel;
    private CancellationTokenSource? _aiBusyFeedbackCts;
    private bool _allowClose;
    private static readonly double[] AiBusyShakeOffsets_c = new[] { -12d, 10d, -8d, 6d, -3d, 0d };

    public event EventHandler<string>? HotkeyUpdated;

    public MainWindow()
        : this(CreateRuntimeLoaderViewModel_c())
    {
    }

    public MainWindow(Launcher.App.ViewModels.MainWindowViewModel_o viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.HotkeyUpdated += (_, hotkey) => HotkeyUpdated?.Invoke(this, hotkey);
        _viewModel.RequestHideLauncher += (_, _) => HideLauncher();
        _viewModel.RequestApplicationExit += (_, _) => RequestApplicationExit();
        _viewModel.RequestFocusSearch += (_, _) => SearchBar.FocusSearch();
        _viewModel.RequestFocusOnboardingHotkey += (_, _) => Onboarding.FocusHotkeyInput();
        _viewModel.RequestFocusSettingsPrimary += (_, _) => Settings.FocusPrimary();
        _viewModel.AiBusyEnterFeedbackRequested += OnAiBusyEnterFeedbackRequested_c;
        _viewModel.OnboardingAddRootErrorRequested += () => Onboarding.TriggerAddRootErrorShake();

        InitializeComponent();
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return _viewModel.InitializeAsync(cancellationToken);
    }

    public void OpenOnboarding()
    {
        _viewModel.OpenOnboarding();
    }

    public void ToggleLauncher()
    {
        if (IsVisible)
        {
            return;
        }

        ShowLauncher();
    }

    public void HideLauncher()
    {
        _viewModel.NotifyWindowHiding();
        _viewModel.CancelSettings();
        _viewModel.HandleOnboardingExit();
        Topmost = false;
        Hide();
    }

    public void ShowLauncher()
    {
        _viewModel.PrepareBeforeShow();
        Show();
        WindowState = WindowState.Normal;
        Topmost = true;
        Activate();
        _viewModel.NotifyWindowShown();
    }

    public void RequestApplicationExit()
    {
        _allowClose = true;
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _aiBusyFeedbackCts?.Cancel();
        _aiBusyFeedbackCts?.Dispose();
        _aiBusyFeedbackCts = null;

        if (!_allowClose)
        {
            e.Cancel = true;
            HideLauncher();
            return;
        }

        _viewModel.HandleOnboardingExit();
        base.OnClosing(e);
    }

    private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel.IsSettingsVisible)
        {
            if (_viewModel.IsExitGesture(e.Key, e.KeyModifiers))
            {
                _viewModel.CancelSettings();
                e.Handled = true;
            }

            return;
        }

        if (_viewModel.IsOnboardingVisible)
        {
            if (_viewModel.IsExitGesture(e.Key, e.KeyModifiers))
            {
                if (_viewModel.IsFirstTimeOnboarding)
                    HideLauncher();
                else
                    _viewModel.HandleOnboardingExit();
                e.Handled = true;
            }

            return;
        }

        if (_viewModel.IsSwitchModeGesture(e.Key, e.KeyModifiers))
        {
            _viewModel.SwitchToNextMode();
            e.Handled = true;
            return;
        }

        if (_viewModel.IsMoveUpGesture(e.Key, e.KeyModifiers))
        {
            _viewModel.MoveSelection(-1);
            Results.ScrollToSelected();
            e.Handled = true;
            return;
        }

        if (_viewModel.IsMoveDownGesture(e.Key, e.KeyModifiers))
        {
            _viewModel.MoveSelection(1);
            Results.ScrollToSelected();
            e.Handled = true;
            return;
        }

        if (_viewModel.IsExitGesture(e.Key, e.KeyModifiers))
        {
            if (_viewModel.TryExitFilePickerIfActive())
            {
                e.Handled = true;
                return;
            }

            HideLauncher();
            e.Handled = true;
            return;
        }

        if (await _viewModel.TryHandleAlternativeLaunchAsync(e.Key, e.KeyModifiers))
        {
            e.Handled = true;
            return;
        }

        if (_viewModel.IsAiChatMode && IsEnterKey_c(e.Key))
        {
            if ((e.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift)
            {
                // Keep newline insertion behavior in AI input box.
                return;
            }

            e.Handled = true;
            await _viewModel.HandleConfirmAsync(KeyModifiers.None);
            return;
        }

        if (_viewModel.IsConfirmGesture(e.Key, e.KeyModifiers))
        {
            e.Handled = true;
            await _viewModel.HandleConfirmAsync(e.KeyModifiers);
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (_allowClose || !IsVisible)
        {
            return;
        }

        HideLauncher();
    }

    private void OnAiBusyEnterFeedbackRequested_c(object? sender, EventArgs e)
    {
        _ = PlayAiBusyEnterFeedbackAsync_c();
    }

    private async Task PlayAiBusyEnterFeedbackAsync_c()
    {
        _aiBusyFeedbackCts?.Cancel();
        _aiBusyFeedbackCts?.Dispose();

        var cts = new CancellationTokenSource();
        _aiBusyFeedbackCts = cts;
        var cancellationToken = cts.Token;
        var shakeTransform = Results.EnsureAiShakeTransform();

        SearchBar.SetBusyRejectActive(true);

        try
        {
            foreach (var offset in AiBusyShakeOffsets_c)
            {
                cancellationToken.ThrowIfCancellationRequested();
                shakeTransform.X = offset;
                await Task.Delay(24, cancellationToken);
            }

            await Task.Delay(90, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_aiBusyFeedbackCts, cts))
            {
                shakeTransform.X = 0d;
                SearchBar.SetBusyRejectActive(false);
                _aiBusyFeedbackCts.Dispose();
                _aiBusyFeedbackCts = null;
            }
        }
    }

    private static bool IsEnterKey_c(Key key)
    {
        return key is Key.Enter or Key.Return;
    }

    private static Launcher.App.ViewModels.MainWindowViewModel_o CreateRuntimeLoaderViewModel_c()
    {
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

        return new Launcher.App.ViewModels.MainWindowViewModel_o(application);
    }
}
