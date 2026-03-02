using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Launcher.App.Models;
using Launcher.App.Services;
using Launcher.Application.Models;
using LiveMarkdown.Avalonia;

namespace Launcher.App.ViewModels;

public sealed class MainWindowViewModel_o : ViewModelBase_c, IDisposable
{
    private readonly Launcher.Application.Runtime.LauncherApplication_o _application;
    private readonly List<Launcher.Core.Models.SearchResult_c> _currentResults = [];
    private readonly List<MetaCommandSuggestion_c> _currentMetaSuggestions = [];
    private readonly ToolIconResolver_c _toolIconResolver = new();
    private readonly List<Launcher.Core.Models.ToolRecord_c> _onboardingDetectedTools = [];
    private readonly List<FilePickerEntry_c> _filePickerEntries = [];
    private readonly List<FilePickerEntry_c> _filePickerVisibleEntries = [];

    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _filePickerLoadCts;
    private bool _isInitialized;
    private bool _isMetaCommandMode;
    private bool _isOnboardingVisible;
    private bool _isSettingsVisible;
    private bool _isAiRequestInFlight;
    private bool _suppressSearchTextChange;
    private string _searchText = string.Empty;
    private string _statusText = "Ready";
    private string _commandHintText = "projects | > command | ! meta";
    private string _searchWatermarkText = "Search projects, > command, or ! meta command";
    private string _searchPrefixText = ">";
    private string _alternativeLaunchModifierLabelText = "Shift+Alt";
    private string _filePickerReturnSearchText = string.Empty;
    private int _selectedIndex = -1;
    private LauncherMode_c _currentMode = LauncherMode_c.ProjectSearch;

    private Launcher.Core.Models.ProjectRecord_c? _filePickerProject;
    private Launcher.Core.Models.ToolRecord_c? _filePickerTool;
    private InAppHotkeyRouter_c _hotkeyRouter = new(Launcher.Core.Models.GeneralHotkeySettings_c.CreateDefault());
    private static readonly HashSet<string> KnownTextFileExtensions_c = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".sln", ".props", ".targets", ".xaml", ".axaml",
        ".json", ".md", ".txt", ".xml", ".yaml", ".yml", ".toml", ".ini", ".config",
        ".editorconfig", ".gitignore", ".gitattributes",
        ".js", ".ts", ".jsx", ".tsx", ".css", ".scss", ".less", ".html", ".htm",
        ".ps1", ".cmd", ".bat", ".sh", ".sql", ".resx", ".csv", ".log"
    };
    private static readonly HashSet<string> KnownBinaryFileExtensions_c = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".pdb", ".so", ".dylib", ".bin", ".dat", ".obj", ".class",
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp",
        ".zip", ".7z", ".rar", ".gz", ".tar",
        ".mp3", ".wav", ".ogg", ".mp4", ".avi", ".mov", ".mkv",
        ".pdf", ".woff", ".woff2", ".eot", ".ttf", ".otf"
    };

    private string _onboardingAddRootText = string.Empty;
    private string _onboardingHotkeyText = "Alt+Shift+Space";
    private string _onboardingToolCheckStatusText = "Checking installed tools...";
    private bool _onboardingFinishEnabled = true;
    private string _onboardingErrorText = string.Empty;
    private bool _hasOnboardingError;
    private Launcher.Core.Models.ToolRecord_c? _selectedPreferredTool;

    private string _settingsGeneralExitText = "Escape";
    private string _settingsGeneralMoveUpText = "Up";
    private string _settingsGeneralMoveDownText = "Down";
    private string _settingsGeneralConfirmText = "Enter";
    private string _settingsGeneralSwitchModeText = "Shift+Tab";
    private string _settingsGeneralAlternativeModifiersText = "Shift+Alt";
    private string _settingsGeneralAlternativeKeysText = "Z, X, C, V, B, N, M";
    private string _settingsThemeOverrideText = Launcher.Core.Models.ThemeOverrideOptions_c.Default;
    private string _settingsTerminalShellExecutableText = "powershell.exe";
    private string _settingsTerminalShellArgumentsPrefixText = "-NoLogo -NoProfile -Command";
    private string _settingsAiChatCliExecutableText = string.Empty;
    private string _settingsAiChatArgumentTemplateText = "--prompt {prompt}";
    private string _settingsAiChatContextDirectoryText = string.Empty;
    private string _settingsAiChatTimeoutSecondsText = "45";
    private string _settingsErrorText = string.Empty;
    private bool _hasSettingsError;

    private bool _settingsBehaviorPersistModeOnLaunch;
    private bool _settingsBehaviorPersistProjectSearchState;
    private bool _settingsBehaviorPersistAiChatState;
    private bool _settingsBehaviorPersistTerminalState;
    private LauncherMode_c _lastActiveMode = LauncherMode_c.ProjectSearch;
    private string _lastProjectSearchText = string.Empty;
    private static readonly string AiStateTempPath =
        Path.Combine(Path.GetTempPath(), "launcher-ai-state.tmp");
    private static readonly string TerminalStateTempPath =
        Path.Combine(Path.GetTempPath(), "launcher-terminal-state.tmp");

    public event EventHandler<string>? HotkeyUpdated;
    public event EventHandler? RequestHideLauncher;
    public event EventHandler? RequestApplicationExit;
    public event EventHandler? RequestFocusSearch;
    public event EventHandler? RequestFocusOnboardingHotkey;
    public event EventHandler? RequestFocusSettingsPrimary;
    public event EventHandler? AiBusyEnterFeedbackRequested;

    public ObservableCollection<DisplayRow_c> DisplayRows { get; } = [];

    public ObservableCollection<IdeOptionItem_c> DefaultIdeOptionItems { get; } = [];

    public ObservableCollection<IdeOptionItem_c> AlternateIdeOptionItems { get; } = [];

    public ObservableCollection<string> OnboardingToolRows { get; } = [];

    public ObservableCollection<Launcher.Core.Models.ToolRecord_c> OnboardingDetectedTools { get; } = [];

    public ObservableStringBuilder AiMarkdownBuilder { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty_c(ref _searchText, value))
            {
                return;
            }

            if (_suppressSearchTextChange || !_isInitialized)
            {
                return;
            }

            HandleSearchTextChanged_c();
        }
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (!SetProperty_c(ref _selectedIndex, value))
            {
                return;
            }

            UpdateSelectionDependentState_c();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty_c(ref _statusText, value);
    }

    public string CommandHintText
    {
        get => _commandHintText;
        private set => SetProperty_c(ref _commandHintText, value);
    }

    public string SearchWatermarkText
    {
        get => _searchWatermarkText;
        private set => SetProperty_c(ref _searchWatermarkText, value);
    }

    public string SearchPrefixText
    {
        get => _searchPrefixText;
        private set => SetProperty_c(ref _searchPrefixText, value);
    }

    public string ModeIndicatorIconText => IsMetaCommandMode
        ? "🛠"
        : CurrentMode switch
        {
            LauncherMode_c.AiChat => "🤖",
            LauncherMode_c.Terminal => "💻",
            _ => "🔍"
        };

    public string AlternativeLaunchModifierLabelText
    {
        get => _alternativeLaunchModifierLabelText;
        private set => SetProperty_c(ref _alternativeLaunchModifierLabelText, value);
    }

    public LauncherMode_c CurrentMode
    {
        get => _currentMode;
        private set
        {
            if (!SetProperty_c(ref _currentMode, value))
            {
                return;
            }

            OnPropertyChanged_c(nameof(IsProjectSearchMode));
            OnPropertyChanged_c(nameof(IsAiChatMode));
            OnPropertyChanged_c(nameof(IsTerminalMode));
            OnPropertyChanged_c(nameof(IsFilePickerMode));
            OnPropertyChanged_c(nameof(IsTopModeNavigationVisible));
            OnPropertyChanged_c(nameof(IsTopFilePickerHintVisible));
            OnPropertyChanged_c(nameof(IsResultListVisible));
            OnPropertyChanged_c(nameof(IsAiOutputVisible));
            OnPropertyChanged_c(nameof(IsAiResponseBlocked));
            OnPropertyChanged_c(nameof(IsSearchEnabled));
            OnPropertyChanged_c(nameof(IsResultListEnabled));
            OnPropertyChanged_c(nameof(SearchInputHostHeight));
            OnPropertyChanged_c(nameof(SearchInputTextWrapping));
            OnPropertyChanged_c(nameof(SearchInputMinLines));
            OnPropertyChanged_c(nameof(SearchInputMaxLines));
            OnPropertyChanged_c(nameof(SearchInputVerticalContentAlignment));
            OnPropertyChanged_c(nameof(SearchPrefixHeight));
            OnPropertyChanged_c(nameof(SearchPrefixVerticalAlignment));
            OnPropertyChanged_c(nameof(ModeBadgeText));
            OnPropertyChanged_c(nameof(ModeIndicatorIconText));
            OnFooterVisibilityStateChanged_c();
            RefreshDynamicUiText_c();
        }
    }

    public bool IsProjectSearchMode => CurrentMode == LauncherMode_c.ProjectSearch;

    public bool IsAiChatMode => CurrentMode == LauncherMode_c.AiChat;

    public bool IsTerminalMode => CurrentMode == LauncherMode_c.Terminal;

    public bool IsFilePickerMode => CurrentMode == LauncherMode_c.FilePicker;

    public bool IsTopModeNavigationVisible => !IsFilePickerMode;

    public bool IsTopFilePickerHintVisible => IsFilePickerMode;

    public string ModeBadgeText => CurrentMode switch
    {
        LauncherMode_c.ProjectSearch => "mode: project",
        LauncherMode_c.AiChat => "mode: ai",
        LauncherMode_c.Terminal => "mode: terminal",
        LauncherMode_c.FilePicker => "mode: file picker",
        _ => "mode: project"
    };

    public bool IsMetaCommandMode
    {
        get => _isMetaCommandMode;
        private set
        {
            if (!SetProperty_c(ref _isMetaCommandMode, value))
            {
                return;
            }

            OnPropertyChanged_c(nameof(ModeIndicatorIconText));
            OnFooterVisibilityStateChanged_c();
            RefreshDynamicUiText_c();
        }
    }

    public bool IsFooterVisible => !IsOnboardingVisible && !IsSettingsVisible;

    public bool IsFooterMetaHintVisible => IsFooterVisible && IsMetaCommandMode;

    public bool IsFooterIdeOptionsVisible => IsFooterVisible && IsProjectSearchMode && !IsMetaCommandMode;

    public bool IsFooterTerminalHintVisible => IsFooterVisible && IsTerminalMode && !IsMetaCommandMode;

    public bool IsFooterAiHintVisible => IsFooterVisible && IsAiChatMode && !IsMetaCommandMode;

    public bool IsFooterFilePickerHintVisible => IsFooterVisible && IsFilePickerMode && !IsMetaCommandMode;

    public bool IsFooterStatusVisible => IsFooterVisible;

public bool IsIdeOptionsVisible => IsFooterIdeOptionsVisible;

    public bool IsOnboardingVisible
    {
        get => _isOnboardingVisible;
        private set
        {
            if (!SetProperty_c(ref _isOnboardingVisible, value))
            {
                return;
            }

            OnPropertyChanged_c(nameof(IsSearchEnabled));
            OnPropertyChanged_c(nameof(IsResultListEnabled));
            OnPropertyChanged_c(nameof(IsResultListVisible));
            OnPropertyChanged_c(nameof(IsAiOutputVisible));
            OnPropertyChanged_c(nameof(IsAiResponseBlocked));
            OnFooterVisibilityStateChanged_c();
        }
    }

    public bool IsFirstTimeOnboarding => _application.IsOnboardingPending();

    public bool IsSettingsVisible
    {
        get => _isSettingsVisible;
        private set
        {
            if (!SetProperty_c(ref _isSettingsVisible, value))
            {
                return;
            }

            OnPropertyChanged_c(nameof(IsSearchEnabled));
            OnPropertyChanged_c(nameof(IsResultListEnabled));
            OnPropertyChanged_c(nameof(IsResultListVisible));
            OnPropertyChanged_c(nameof(IsAiOutputVisible));
            OnPropertyChanged_c(nameof(IsAiResponseBlocked));
            OnFooterVisibilityStateChanged_c();
        }
    }

    public bool IsSearchEnabled => !IsOnboardingVisible && !IsSettingsVisible;

    public bool IsResultListEnabled => !IsOnboardingVisible && !IsSettingsVisible && !IsAiChatMode;

    public bool IsResultListVisible => !IsOnboardingVisible && !IsSettingsVisible && !IsAiChatMode;

    public bool IsAiOutputVisible => !IsOnboardingVisible && !IsSettingsVisible && IsAiChatMode;

    public bool IsAiResponseBlocked => IsAiOutputVisible && _isAiRequestInFlight;

    public double SearchInputHostHeight => IsAiChatMode ? 104d : 52d;

    public TextWrapping SearchInputTextWrapping => IsAiChatMode ? TextWrapping.Wrap : TextWrapping.NoWrap;

    public int SearchInputMinLines => IsAiChatMode ? 3 : 1;

    public int SearchInputMaxLines => IsAiChatMode ? 3 : 1;

    public VerticalAlignment SearchInputVerticalContentAlignment => IsAiChatMode
        ? VerticalAlignment.Top
        : VerticalAlignment.Center;

    public double SearchPrefixHeight => IsAiChatMode ? double.NaN : 28d;

    public VerticalAlignment SearchPrefixVerticalAlignment => IsAiChatMode
        ? VerticalAlignment.Stretch
        : VerticalAlignment.Center;

    public ObservableCollection<OnboardingRootItem_c> OnboardingRoots { get; } = [];

    public event Action? OnboardingAddRootErrorRequested;

    public string OnboardingAddRootText
    {
        get => _onboardingAddRootText;
        set => SetProperty_c(ref _onboardingAddRootText, value);
    }

    public string OnboardingHotkeyText
    {
        get => _onboardingHotkeyText;
        set => SetProperty_c(ref _onboardingHotkeyText, value);
    }

    public string OnboardingToolCheckStatusText
    {
        get => _onboardingToolCheckStatusText;
        private set => SetProperty_c(ref _onboardingToolCheckStatusText, value);
    }

    public bool OnboardingFinishEnabled
    {
        get => _onboardingFinishEnabled;
        private set => SetProperty_c(ref _onboardingFinishEnabled, value);
    }

    public string OnboardingErrorText
    {
        get => _onboardingErrorText;
        private set => SetProperty_c(ref _onboardingErrorText, value);
    }

    public bool HasOnboardingError
    {
        get => _hasOnboardingError;
        private set => SetProperty_c(ref _hasOnboardingError, value);
    }

    public Launcher.Core.Models.ToolRecord_c? SelectedPreferredTool
    {
        get => _selectedPreferredTool;
        set => SetProperty_c(ref _selectedPreferredTool, value);
    }

    public string SettingsGeneralExitText
    {
        get => _settingsGeneralExitText;
        set => SetProperty_c(ref _settingsGeneralExitText, value);
    }

    public string SettingsGeneralMoveUpText
    {
        get => _settingsGeneralMoveUpText;
        set => SetProperty_c(ref _settingsGeneralMoveUpText, value);
    }

    public string SettingsGeneralMoveDownText
    {
        get => _settingsGeneralMoveDownText;
        set => SetProperty_c(ref _settingsGeneralMoveDownText, value);
    }

    public string SettingsGeneralConfirmText
    {
        get => _settingsGeneralConfirmText;
        set => SetProperty_c(ref _settingsGeneralConfirmText, value);
    }

    public string SettingsGeneralSwitchModeText
    {
        get => _settingsGeneralSwitchModeText;
        set => SetProperty_c(ref _settingsGeneralSwitchModeText, value);
    }

    public string SettingsGeneralAlternativeModifiersText
    {
        get => _settingsGeneralAlternativeModifiersText;
        set => SetProperty_c(ref _settingsGeneralAlternativeModifiersText, value);
    }

    public string SettingsGeneralAlternativeKeysText
    {
        get => _settingsGeneralAlternativeKeysText;
        set => SetProperty_c(ref _settingsGeneralAlternativeKeysText, value);
    }

    public IReadOnlyList<string> SettingsThemeOverrideOptions { get; } =
    [
        Launcher.Core.Models.ThemeOverrideOptions_c.Default,
        Launcher.Core.Models.ThemeOverrideOptions_c.Light,
        Launcher.Core.Models.ThemeOverrideOptions_c.Dark
    ];

    public string SettingsThemeOverrideText
    {
        get => _settingsThemeOverrideText;
        set => SetProperty_c(ref _settingsThemeOverrideText, value);
    }

    public string SettingsTerminalShellExecutableText
    {
        get => _settingsTerminalShellExecutableText;
        set => SetProperty_c(ref _settingsTerminalShellExecutableText, value);
    }

    public string SettingsTerminalShellArgumentsPrefixText
    {
        get => _settingsTerminalShellArgumentsPrefixText;
        set => SetProperty_c(ref _settingsTerminalShellArgumentsPrefixText, value);
    }

    public string SettingsAiChatCliExecutableText
    {
        get => _settingsAiChatCliExecutableText;
        set => SetProperty_c(ref _settingsAiChatCliExecutableText, value);
    }

    public string SettingsAiChatArgumentTemplateText
    {
        get => _settingsAiChatArgumentTemplateText;
        set => SetProperty_c(ref _settingsAiChatArgumentTemplateText, value);
    }

    public string SettingsAiChatContextDirectoryText
    {
        get => _settingsAiChatContextDirectoryText;
        set => SetProperty_c(ref _settingsAiChatContextDirectoryText, value);
    }

    public string SettingsAiChatTimeoutSecondsText
    {
        get => _settingsAiChatTimeoutSecondsText;
        set => SetProperty_c(ref _settingsAiChatTimeoutSecondsText, value);
    }

    public string SettingsErrorText
    {
        get => _settingsErrorText;
        private set => SetProperty_c(ref _settingsErrorText, value);
    }

    public bool HasSettingsError
    {
        get => _hasSettingsError;
        private set => SetProperty_c(ref _hasSettingsError, value);
    }

    public bool SettingsBehaviorPersistModeOnLaunch
    {
        get => _settingsBehaviorPersistModeOnLaunch;
        set => SetProperty_c(ref _settingsBehaviorPersistModeOnLaunch, value);
    }

    public bool SettingsBehaviorPersistProjectSearchState
    {
        get => _settingsBehaviorPersistProjectSearchState;
        set => SetProperty_c(ref _settingsBehaviorPersistProjectSearchState, value);
    }

    public bool SettingsBehaviorPersistAiChatState
    {
        get => _settingsBehaviorPersistAiChatState;
        set => SetProperty_c(ref _settingsBehaviorPersistAiChatState, value);
    }

    public bool SettingsBehaviorPersistTerminalState
    {
        get => _settingsBehaviorPersistTerminalState;
        set => SetProperty_c(ref _settingsBehaviorPersistTerminalState, value);
    }

    public MainWindowViewModel_o(Launcher.Application.Runtime.LauncherApplication_o application)
    {
        _application = application;
        SetNoIdeOptions_c("select an item");
        RefreshDynamicUiText_c();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _application.InitializeAsync(cancellationToken);
        _application.IndexUpdated += OnInitialIndexUpdated_c;
        ApplyThemeOverride_c(_application.Settings.ThemeOverride);
        _hotkeyRouter = new InAppHotkeyRouter_c(_application.Settings.GeneralHotkeys);
        LoadSettingsDraftFromRuntime_c();
        RefreshDynamicUiText_c();
        _isInitialized = true;

        if (_application.IsOnboardingPending())
        {
            await EnterOnboardingModeAsync_c();
            return;
        }

        CurrentMode = LauncherMode_c.ProjectSearch;
        await RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
        RequestHideLauncher?.Invoke(this, EventArgs.Empty);
    }

    public void OpenOnboarding()
    {
        _ = EnterOnboardingModeAsync_c();
    }

    public void OpenSettings()
    {
        if (IsOnboardingVisible)
        {
            return;
        }

        LoadSettingsDraftFromRuntime_c();
        ClearSettingsError_c();
        IsSettingsVisible = true;
        RequestFocusSettingsPrimary?.Invoke(this, EventArgs.Empty);
        UpdateStatus_c("Opened.");
    }

    public void CancelSettings()
    {
        if (!IsSettingsVisible)
        {
            return;
        }

        IsSettingsVisible = false;
        ClearSettingsError_c();
        RequestFocusSearch?.Invoke(this, EventArgs.Empty);
        UpdateStatus_c("Changes discarded.");
    }

    public void SaveSettings()
    {
        if (!IsSettingsVisible)
        {
            return;
        }

        ClearSettingsError_c();
        if (!TryBuildGeneralHotkeysFromSettingsDraft_c(out var generalHotkeys, out var hotkeyError))
        {
            SetSettingsError_c(hotkeyError ?? "Invalid general hotkey settings.");
            return;
        }

        var shellExecutable = SettingsTerminalShellExecutableText.Trim();
        if (string.IsNullOrWhiteSpace(shellExecutable))
        {
            SetSettingsError_c("Terminal shell executable is required.");
            return;
        }

        var shellPrefix = TokenizeCommandText_c(SettingsTerminalShellArgumentsPrefixText);
        if (shellPrefix.Count == 0)
        {
            shellPrefix = Launcher.Core.Models.TerminalSettings_c.BuildDefaultShellArgumentPrefix_c();
        }

        var argumentTemplate = string.IsNullOrWhiteSpace(SettingsAiChatArgumentTemplateText)
            ? "--prompt {prompt}"
            : SettingsAiChatArgumentTemplateText.Trim();
        if (!argumentTemplate.Contains("{prompt}", StringComparison.Ordinal))
        {
            SetSettingsError_c("AI argument template must include {prompt}.");
            return;
        }

        if (!int.TryParse(SettingsAiChatTimeoutSecondsText.Trim(), out var timeoutSeconds))
        {
            SetSettingsError_c("AI timeout must be a number.");
            return;
        }

        if (!TryNormalizeThemeOverride_c(SettingsThemeOverrideText, out var themeOverride))
        {
            SetSettingsError_c("Theme override must be Default, Light, or Dark.");
            return;
        }

        timeoutSeconds = Math.Clamp(timeoutSeconds, 5, 300);
        var contextDirectory = SettingsAiChatContextDirectoryText.Trim();
        if (!string.IsNullOrWhiteSpace(contextDirectory))
        {
            try
            {
                contextDirectory = Path.GetFullPath(contextDirectory);
            }
            catch
            {
                SetSettingsError_c("AI context directory path is invalid.");
                return;
            }

            if (!Directory.Exists(contextDirectory))
            {
                SetSettingsError_c("AI context directory does not exist.");
                return;
            }
        }

        var previousHotkey = _application.Settings.Hotkeys.Toggle;
        _application.Settings.ThemeOverride = themeOverride;
        _application.Settings.GeneralHotkeys = generalHotkeys;
        _application.Settings.Terminal = new Launcher.Core.Models.TerminalSettings_c
        {
            ShellExecutable = shellExecutable,
            ShellArgumentsPrefix = shellPrefix
        };
        _application.Settings.AiChat = new Launcher.Core.Models.AiChatSettings_c
        {
            CliExecutable = SettingsAiChatCliExecutableText.Trim(),
            ArgumentTemplate = argumentTemplate,
            ContextDirectory = contextDirectory,
            TimeoutSeconds = timeoutSeconds
        };
        _application.Settings.Behavior = new Launcher.Core.Models.BehaviorSettings_c
        {
            PersistModeOnLaunch = SettingsBehaviorPersistModeOnLaunch,
            PersistProjectSearchState = SettingsBehaviorPersistProjectSearchState,
            PersistAiChatState = SettingsBehaviorPersistAiChatState,
            PersistTerminalState = SettingsBehaviorPersistTerminalState
        };

        var saveResult = _application.PersistSettings();
        if (!saveResult.IsSuccess)
        {
            SetSettingsError_c(saveResult.Message);
            return;
        }

        _hotkeyRouter = new InAppHotkeyRouter_c(_application.Settings.GeneralHotkeys);
        ApplyThemeOverride_c(_application.Settings.ThemeOverride);
        RefreshDynamicUiText_c();
        EmitHotkeyUpdateIfChanged_c(previousHotkey);
        IsSettingsVisible = false;
        RequestFocusSearch?.Invoke(this, EventArgs.Empty);
        UpdateStatus_c("Saved.");
        _ = RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
    }

    public void HandleOnboardingExit()
    {
        if (!IsOnboardingVisible)
        {
            return;
        }

        var previousHotkey = _application.Settings.Hotkeys.Toggle;
        if (_application.IsOnboardingPending())
        {
            var result = _application.ApplyPendingOnboardingDefaults();
            UpdateStatus_c(result.Message);
        }

        ExitOnboardingMode_c();
        EmitHotkeyUpdateIfChanged_c(previousHotkey);
    }

    public bool IsMoveUpGesture(Key key, KeyModifiers modifiers)
    {
        return _hotkeyRouter.IsMoveUp(key, modifiers);
    }

    public bool IsMoveDownGesture(Key key, KeyModifiers modifiers)
    {
        return _hotkeyRouter.IsMoveDown(key, modifiers);
    }

    public bool IsConfirmGesture(Key key, KeyModifiers modifiers)
    {
        return _hotkeyRouter.IsConfirm(key, modifiers);
    }

    public bool IsExitGesture(Key key, KeyModifiers modifiers)
    {
        return _hotkeyRouter.IsExit(key, modifiers);
    }

    public bool IsSwitchModeGesture(Key key, KeyModifiers modifiers)
    {
        return _hotkeyRouter.IsSwitchMode(key, modifiers);
    }

    public void MoveSelection(int delta)
    {
        var itemCount = GetListItemCount_c();
        if (itemCount == 0)
        {
            return;
        }

        if (SelectedIndex < 0)
        {
            SelectedIndex = 0;
            return;
        }

        SelectedIndex = Math.Clamp(SelectedIndex + delta, 0, itemCount - 1);
    }

    public void SwitchToNextMode()
    {
        if (IsOnboardingVisible)
        {
            return;
        }

        if (IsProjectSearchMode && IsMetaCycleActive_c())
        {
            ExitMetaCycleMode_c();
            return;
        }

        if (IsTerminalMode)
        {
            EnterMetaCycleMode_c();
            return;
        }

        var nextMode = CurrentMode switch
        {
            LauncherMode_c.ProjectSearch => LauncherMode_c.AiChat,
            LauncherMode_c.AiChat => LauncherMode_c.Terminal,
            LauncherMode_c.FilePicker => LauncherMode_c.AiChat,
            _ => LauncherMode_c.ProjectSearch
        };

        _ = SetModeAsync_c(nextMode, preserveInput: false);
    }

    public async Task NavigateToProjectModeAsync()
    {
        if (IsOnboardingVisible || IsSettingsVisible)
        {
            return;
        }

        if (IsFilePickerMode)
        {
            ExitFilePickerMode_c();
            return;
        }

        if (IsProjectSearchMode && IsMetaCycleActive_c())
        {
            ExitMetaCycleMode_c();
            return;
        }

        if (IsAiChatMode)
        {
            SetSearchTextWithoutEvent_c(string.Empty);
            await SetModeAsync_c(LauncherMode_c.ProjectSearch, preserveInput: true);
            return;
        }

        await SetModeAsync_c(LauncherMode_c.ProjectSearch, preserveInput: false);
    }

    public Task NavigateToTerminalModeAsync()
    {
        if (IsOnboardingVisible || IsSettingsVisible)
        {
            return Task.CompletedTask;
        }

        return SetModeAsync_c(LauncherMode_c.Terminal, preserveInput: false);
    }

    public Task NavigateToAiModeAsync()
    {
        if (IsOnboardingVisible || IsSettingsVisible)
        {
            return Task.CompletedTask;
        }

        return SetModeAsync_c(LauncherMode_c.AiChat, preserveInput: false);
    }

    public void NavigateToMetaMode()
    {
        if (IsOnboardingVisible || IsSettingsVisible)
        {
            return;
        }

        EnterMetaCycleMode_c();
    }

    public bool TryExitFilePickerIfActive()
    {
        if (!IsFilePickerMode)
        {
            return false;
        }

        ExitFilePickerMode_c();
        return true;
    }

    public async Task<bool> TryHandleAlternativeLaunchAsync(Key key, KeyModifiers modifiers)
    {
        if (!IsProjectSearchMode)
        {
            return false;
        }

        var slot = _hotkeyRouter.ResolveAlternativeSlot(key, modifiers);
        if (slot < 0)
        {
            return false;
        }

        await LaunchSelectedAsync_c(slot);
        return true;
    }

    public async Task HandleConfirmAsync(KeyModifiers modifiers)
    {
        if (IsAiChatMode)
        {
            if ((modifiers & KeyModifiers.Shift) == KeyModifiers.Shift)
            {
                return;
            }

            await SendAiPromptAsync_c();
            return;
        }

        if (IsFilePickerMode)
        {
            await LaunchSelectedFileFromPickerAsync_c();
            return;
        }

        var rawInput = SearchText.Trim();
        if (rawInput.StartsWith("!", StringComparison.Ordinal))
        {
            await ExecuteMetaCommandAsync_c(rawInput);
            return;
        }

        if (rawInput.StartsWith('>'))
        {
            await ExecuteTerminalCommandAsync_c(rawInput);
            return;
        }

        await LaunchSelectedAsync_c(alternativeIndex: null);
    }

    public Task HandleEnterAsync()
    {
        return HandleConfirmAsync(KeyModifiers.None);
    }

    public async Task HandleIdeOptionClickedAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !IsProjectSearchMode)
        {
            return;
        }

        if (string.Equals(token, "default", StringComparison.OrdinalIgnoreCase))
        {
            await LaunchSelectedAsync_c(alternativeIndex: null);
            return;
        }

        if (token.StartsWith("alt:", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(token[4..], out var index) &&
            index >= 0)
        {
            await LaunchSelectedAsync_c(index);
        }
    }

    public void AddOnboardingRoot()
    {
        var input = OnboardingAddRootText.Trim();
        if (string.IsNullOrWhiteSpace(input)) { OnboardingAddRootErrorRequested?.Invoke(); return; }
        try { input = System.IO.Path.GetFullPath(input); } catch { OnboardingAddRootErrorRequested?.Invoke(); return; }
        if (!Directory.Exists(input)) { OnboardingAddRootErrorRequested?.Invoke(); return; }
        if (OnboardingRoots.Any(r => string.Equals(r.Path, input, StringComparison.OrdinalIgnoreCase))) return;
        OnboardingRoots.Add(new OnboardingRootItem_c { Path = input, Enabled = true });
        OnboardingAddRootText = string.Empty;
    }

    public void RemoveOnboardingRoot(OnboardingRootItem_c item) => OnboardingRoots.Remove(item);

    public void ClearOnboardingRoots() => OnboardingRoots.Clear();

    public void CompleteOnboarding()
    {
        ClearOnboardingError_c();
        var selectedRoots = CollectOnboardingRoots_c();
        if (selectedRoots.Count == 0)
        {
            SetOnboardingError_c("Select at least one existing root path.");
            return;
        }

        var hotkeyText = OnboardingHotkeyText.Trim();
        if (!GlobalHotkeyService_c.IsValidHotkeyText(hotkeyText))
        {
            SetOnboardingError_c("Hotkey invalid. Example: Alt+Shift+Space.");
            return;
        }

        var preferredTool = SelectedPreferredTool;
        var previousHotkey = _application.Settings.Hotkeys.Toggle;
        var finishResult = _application.CompleteOnboarding(selectedRoots, hotkeyText, preferredTool?.ToolId);
        if (!finishResult.IsSuccess)
        {
            SetOnboardingError_c(finishResult.Message);
            return;
        }

        _hotkeyRouter = new InAppHotkeyRouter_c(_application.Settings.GeneralHotkeys);
        UpdateStatus_c(finishResult.Message);
        ExitOnboardingMode_c();
        EmitHotkeyUpdateIfChanged_c(previousHotkey);
    }

    public void SkipOnboarding()
    {
        ClearOnboardingError_c();
        var previousHotkey = _application.Settings.Hotkeys.Toggle;
        var skipResult = _application.SkipOnboarding();
        UpdateStatus_c(skipResult.Message);
        ExitOnboardingMode_c();
        EmitHotkeyUpdateIfChanged_c(previousHotkey);
    }

    public void NotifyWindowHiding()
    {
        var behavior = _application.Settings.Behavior;
        if (!IsFilePickerMode)
            _lastActiveMode = CurrentMode;
        if (IsProjectSearchMode)
            _lastProjectSearchText = SearchText;
        if (behavior.PersistAiChatState)
            SaveAiStateToTempFile_c();
        if (behavior.PersistTerminalState)
            SaveTerminalStateToTempFile_c();
    }

    public void PrepareBeforeShow()
    {
        if (IsOnboardingVisible || IsSettingsVisible) return;

        var behavior = _application.Settings.Behavior;
        var targetMode = behavior.PersistModeOnLaunch ? _lastActiveMode : LauncherMode_c.ProjectSearch;

        CancelScheduledProjectionRefresh_c();
        IsMetaCommandMode = false;
        ResetAiOutputOnly_c();

        if (targetMode == LauncherMode_c.Terminal)
            SetSearchTextWithoutEvent_c(">");
        else if (targetMode == LauncherMode_c.AiChat)
            SetSearchTextWithoutEvent_c("?");
        else
            SetSearchTextWithoutEvent_c(string.Empty);

        CurrentMode = targetMode;

        if (behavior.PersistModeOnLaunch)
        {
            if (targetMode == LauncherMode_c.ProjectSearch && behavior.PersistProjectSearchState)
                SearchText = _lastProjectSearchText;
            else if (targetMode == LauncherMode_c.AiChat && behavior.PersistAiChatState)
                RestoreAiStateFromTempFile_c();
            else if (targetMode == LauncherMode_c.Terminal && behavior.PersistTerminalState)
                RestoreTerminalStateFromTempFile_c();
        }

        if (targetMode == LauncherMode_c.Terminal)
            RenderTerminalRows_c(_application.GetTerminalOutputSnapshot());

        _ = RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
    }

    public void NotifyWindowShown()
    {
        if (IsOnboardingVisible)
        {
            RequestFocusOnboardingHotkey?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (IsSettingsVisible)
        {
            RequestFocusSettingsPrimary?.Invoke(this, EventArgs.Empty);
            return;
        }

        RequestFocusSearch?.Invoke(this, EventArgs.Empty);
    }

    private void OnInitialIndexUpdated_c(object? sender, EventArgs e)
    {
        _application.IndexUpdated -= OnInitialIndexUpdated_c;
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(
            () => _ = RefreshProjectionForCurrentModeAsync_c(CancellationToken.None));
    }

    public void Dispose()
    {
        _application.IndexUpdated -= OnInitialIndexUpdated_c;
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _filePickerLoadCts?.Cancel();
        _filePickerLoadCts?.Dispose();
        _application.Dispose();
    }

    private void HandleSearchTextChanged_c()
    {
        if (IsOnboardingVisible || IsSettingsVisible)
        {
            return;
        }

        var trimmed = SearchText.TrimStart();

        if (IsFilePickerMode)
        {
            ScheduleProjectionRefresh_c();
            return;
        }

        if (trimmed.StartsWith('?'))
        {
            if (!IsAiChatMode)
            {
                _ = SetModeAsync_c(LauncherMode_c.AiChat, preserveInput: true);
                return;
            }

            ScheduleProjectionRefresh_c();
            return;
        }

        if (trimmed.StartsWith("!", StringComparison.Ordinal))
        {
            if (IsTerminalMode)
            {
                _ = SetModeAsync_c(LauncherMode_c.ProjectSearch, preserveInput: true);
                return;
            }

            ScheduleProjectionRefresh_c();
            return;
        }

        if (trimmed.StartsWith('>'))
        {
            if (!IsTerminalMode)
            {
                _ = SetModeAsync_c(LauncherMode_c.Terminal, preserveInput: true);
                return;
            }

            ScheduleProjectionRefresh_c();
            return;
        }

        if (IsAiChatMode)
        {
            _ = SetModeAsync_c(LauncherMode_c.ProjectSearch, preserveInput: true);
            return;
        }

        if (IsTerminalMode)
        {
            _ = SetModeAsync_c(LauncherMode_c.ProjectSearch, preserveInput: true);
            return;
        }

        if (IsProjectSearchMode)
        {
            ScheduleProjectionRefresh_c();
        }
    }

    private async Task SetModeAsync_c(LauncherMode_c targetMode, bool preserveInput)
    {
        if (IsOnboardingVisible || IsSettingsVisible)
        {
            return;
        }

        CancelScheduledProjectionRefresh_c();

        if (targetMode == CurrentMode)
        {
            await RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
            return;
        }

        var previousMode = CurrentMode;
        if (!preserveInput)
        {
            if (targetMode == LauncherMode_c.Terminal)
            {
                SetSearchTextWithoutEvent_c(">");
            }
            else if (targetMode == LauncherMode_c.AiChat)
            {
                SetSearchTextWithoutEvent_c("?");
            }
            else if (CurrentMode == LauncherMode_c.Terminal && SearchText.TrimStart().StartsWith('>'))
            {
                SetSearchTextWithoutEvent_c(string.Empty);
            }
            RequestFocusSearch?.Invoke(this, EventArgs.Empty);
        }

        CurrentMode = targetMode;
        IsMetaCommandMode = false;
        if (targetMode == LauncherMode_c.Terminal)
        {
            RenderTerminalRows_c(_application.GetTerminalOutputSnapshot());
            UpdateStatus_c("Ready");
        }

        if (targetMode == LauncherMode_c.AiChat && !IsAiChatSetUp_c() && AiMarkdownBuilder.Length == 0)
        {
            AiMarkdownBuilder.Append("### AI chat not configured\n\nSet the **AI CLI executable** in Settings to use this feature.");
        }

        await RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
    }

    private void ScheduleProjectionRefresh_c()
    {
        CancelScheduledProjectionRefresh_c();
        _searchCts = new CancellationTokenSource();
        _ = RefreshProjectionForCurrentModeAsync_c(_searchCts.Token);
    }

    private void CancelScheduledProjectionRefresh_c()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;
    }

    private async Task RefreshProjectionForCurrentModeAsync_c(CancellationToken cancellationToken)
    {
        if (IsOnboardingVisible || IsSettingsVisible || !_isInitialized)
        {
            return;
        }

        if (IsAiChatMode)
        {
            StatusText = _isAiRequestInFlight ? "Sending AI prompt..." : "NOTE: This is a stateless AI chat session";
            return;
        }

        if (IsTerminalMode)
        {
            await RenderTerminalProjectionAsync_c(cancellationToken);
            return;
        }

        if (IsFilePickerMode)
        {
            await RefreshFilePickerSearchAsync_c(cancellationToken);
            return;
        }

        await RefreshProjectSearchAsync_c(cancellationToken);
    }

    private async Task RefreshProjectSearchAsync_c(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsProjectSearchMode)
            {
                return;
            }

            if (cancellationToken.CanBeCanceled && !IsMetaCommandMode)
            {
                var debounceMs = Math.Max(10, _application.Settings.Search.DebounceMs);
                await Task.Delay(debounceMs, cancellationToken);
            }

            if (!IsProjectSearchMode)
            {
                return;
            }

            var projection = _application.Query(SearchText);
            if (!IsProjectSearchMode)
            {
                return;
            }

            if (projection.IsMetaMode)
            {
                RenderMetaSuggestions_c(projection.MetaSuggestions);
                UpdateStatus_c(projection.MetaSuggestions.Count == 0
                    ? "No meta command"
                    : $"{projection.MetaSuggestions.Count} meta command(s)");
                return;
            }

            RenderResults_c(projection.SearchResults);
            var totalProjects = _application.CurrentProjects.Count;
            if (projection.SearchResults.Count == 0)
            {
                UpdateStatus_c(totalProjects == 0
                    ? "Indexing projects... first scan can take a minute."
                    : "No match");
            }
            else
            {
                UpdateStatus_c($"{projection.SearchResults.Count} shown / {totalProjects} indexed");
            }
        }
        catch (OperationCanceledException)
        {
            // Swallow stale query work.
        }
        catch (Exception ex)
        {
            UpdateStatus_c($"Search error: {ex.Message}");
        }
    }

    private async Task RefreshFilePickerSearchAsync_c(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsFilePickerMode)
            {
                return;
            }

            var debounceMs = Math.Max(10, _application.Settings.Search.DebounceMs);
            await Task.Delay(debounceMs, cancellationToken);
            if (!IsFilePickerMode)
            {
                return;
            }

            RenderFilePickerRows_c();
        }
        catch (OperationCanceledException)
        {
            // Ignore stale file-picker filter pass.
        }
    }

    private async Task RenderTerminalProjectionAsync_c(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsTerminalMode)
            {
                return;
            }

            var debounceMs = Math.Max(10, _application.Settings.Search.DebounceMs);
            await Task.Delay(debounceMs, cancellationToken);
            if (!IsTerminalMode)
            {
                return;
            }

            var raw = SearchText.Trim();
            if (raw.StartsWith("!", StringComparison.Ordinal))
            {
                var projection = _application.Query(raw);
                RenderMetaSuggestions_c(projection.MetaSuggestions);
                UpdateStatus_c(projection.MetaSuggestions.Count == 0
                    ? "No meta command"
                    : $"{projection.MetaSuggestions.Count} meta command(s)");
                return;
            }

            RenderTerminalRows_c(_application.GetTerminalOutputSnapshot());
            UpdateStatus_c("Ready");
        }
        catch (OperationCanceledException)
        {
            // Ignore stale render.
        }
    }

    private void RenderResults_c(IReadOnlyList<Launcher.Core.Models.SearchResult_c> results)
    {
        var previousSelectedPath = GetSelectedProjectPath_c();

        IsMetaCommandMode = false;
        _currentMetaSuggestions.Clear();
        _currentResults.Clear();
        _currentResults.AddRange(results);

        DisplayRows.Clear();
        foreach (var result in results)
        {
            DisplayRows.Add(new DisplayRow_c
            {
                ProjectIdentifier = result.Project.ProjectName,
                FullPath = result.Project.ProjectPath,
                DefaultIdeIcon = _toolIconResolver.Resolve(result.SuggestedTool),
                DefaultIdeName = result.SuggestedTool.DisplayName,
                IsIconVisible = true,
                IsIconSeparatorVisible = true,
                RowKind = DisplayRowKind_c.Project
            });
        }

        if (DisplayRows.Count > 0)
        {
            var previousIndex = FindProjectIndexByPath_c(previousSelectedPath);
            SelectedIndex = previousIndex >= 0 ? previousIndex : 0;
            UpdateIdeOptionsForSelection_c(_currentResults[SelectedIndex]);
        }
        else
        {
            SelectedIndex = -1;
            SetNoIdeOptions_c("none");
        }
    }

    private void RenderMetaSuggestions_c(IReadOnlyList<MetaCommandSuggestion_c> suggestions)
    {
        var previousSelectedCommand = GetSelectedMetaCommand_c();

        IsMetaCommandMode = true;
        _currentResults.Clear();
        _currentMetaSuggestions.Clear();
        _currentMetaSuggestions.AddRange(suggestions);

        DisplayRows.Clear();
        foreach (var suggestion in suggestions)
        {
            DisplayRows.Add(new DisplayRow_c
            {
                ProjectIdentifier = suggestion.Command,
                FullPath = suggestion.Description,
                DefaultIdeIcon = _toolIconResolver.ResolveNeutral(),
                DefaultIdeName = string.Empty,
                IsIconVisible = false,
                IsIconSeparatorVisible = false,
                RowKind = DisplayRowKind_c.Meta
            });
        }

        if (DisplayRows.Count > 0)
        {
            var previousIndex = FindMetaIndexByCommand_c(previousSelectedCommand);
            SelectedIndex = previousIndex >= 0 ? previousIndex : 0;
            SetNoIdeOptions_c("meta");
        }
        else
        {
            SelectedIndex = -1;
            SetNoIdeOptions_c("none");
        }
    }

    private void RenderTerminalRows_c(IReadOnlyList<string> lines)
    {
        IsMetaCommandMode = false;
        _currentResults.Clear();
        _currentMetaSuggestions.Clear();

        DisplayRows.Clear();
        if (lines.Count == 0)
        {
            DisplayRows.Add(new DisplayRow_c
            {
                ProjectIdentifier = "Terminal ready. Type > command and press Enter.",
                FullPath = string.Empty,
                DefaultIdeIcon = _toolIconResolver.ResolveNeutral(),
                DefaultIdeName = string.Empty,
                IsIconVisible = false,
                IsIconSeparatorVisible = false,
                RowKind = DisplayRowKind_c.Terminal
            });
            SelectedIndex = 0;
            SetNoIdeOptions_c("terminal");
            return;
        }

        foreach (var line in lines.TakeLast(200))
        {
            var rowKind = ClassifyTerminalRowKind_c(line);
            DisplayRows.Add(new DisplayRow_c
            {
                ProjectIdentifier = NormalizeTerminalLineForDisplay_c(line, rowKind),
                FullPath = string.Empty,
                DefaultIdeIcon = _toolIconResolver.ResolveNeutral(),
                DefaultIdeName = string.Empty,
                IsIconVisible = false,
                IsIconSeparatorVisible = false,
                RowKind = rowKind
            });
        }

        SelectedIndex = DisplayRows.Count - 1;
        SetNoIdeOptions_c("terminal");
    }

    private static DisplayRowKind_c ClassifyTerminalRowKind_c(string line)
    {
        if (line.StartsWith("(error)", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("(stderr)", StringComparison.OrdinalIgnoreCase))
        {
            return DisplayRowKind_c.TerminalError;
        }

        return DisplayRowKind_c.Terminal;
    }

    private static string NormalizeTerminalLineForDisplay_c(string line, DisplayRowKind_c rowKind)
    {
        if (rowKind != DisplayRowKind_c.TerminalError)
        {
            return line;
        }

        const string standardErrorPrefix = "(stderr)";
        if (!line.StartsWith(standardErrorPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return line;
        }

        return line[standardErrorPrefix.Length..].TrimStart();
    }

    private void RenderFilePickerRows_c()
    {
        IsMetaCommandMode = false;
        _currentResults.Clear();
        _currentMetaSuggestions.Clear();
        var selectedPath = GetSelectedVisibleFilePickerPath_c();

        _filePickerVisibleEntries.Clear();
        _filePickerVisibleEntries.AddRange(BuildFilePickerProjection_c(SearchText));
        DisplayRows.Clear();
        foreach (var entry in _filePickerVisibleEntries)
        {
            DisplayRows.Add(new DisplayRow_c
            {
                ProjectIdentifier = entry.DisplayName,
                FullPath = entry.RelativePath,
                DefaultIdeIcon = _filePickerTool is null ? _toolIconResolver.ResolveNeutral() : _toolIconResolver.Resolve(_filePickerTool),
                DefaultIdeName = _filePickerTool?.DisplayName ?? string.Empty,
                IsIconVisible = _filePickerTool is not null,
                IsIconSeparatorVisible = _filePickerTool is not null,
                RowKind = DisplayRowKind_c.FilePicker
            });
        }

        if (_filePickerVisibleEntries.Count > 0)
        {
            var preservedIndex = FindVisibleFilePickerIndexByPath_c(selectedPath);
            SelectedIndex = preservedIndex >= 0
                ? preservedIndex
                : Math.Clamp(SelectedIndex < 0 ? 0 : SelectedIndex, 0, _filePickerVisibleEntries.Count - 1);
        }
        else
        {
            SelectedIndex = -1;
            DisplayRows.Add(new DisplayRow_c
            {
                ProjectIdentifier = _filePickerEntries.Count == 0 ? "No files found." : "No matching files.",
                FullPath = string.Empty,
                DefaultIdeIcon = _toolIconResolver.ResolveNeutral(),
                DefaultIdeName = string.Empty,
                IsIconVisible = false,
                IsIconSeparatorVisible = false,
                RowKind = DisplayRowKind_c.Empty
            });
            SelectedIndex = 0;
        }

        SetNoIdeOptions_c("picker");
        if (_filePickerEntries.Count == 0)
        {
            UpdateStatus_c("No files found.");
            return;
        }

        if (_filePickerVisibleEntries.Count == 0)
        {
            UpdateStatus_c("No match");
            return;
        }

        UpdateStatus_c($"{_filePickerVisibleEntries.Count} shown / {_filePickerEntries.Count} file(s)");
    }

    private void UpdateSelectionDependentState_c()
    {
        if (IsOnboardingVisible || IsSettingsVisible || IsMetaCommandMode || !IsProjectSearchMode)
        {
            return;
        }

        if (SelectedIndex < 0 || SelectedIndex >= _currentResults.Count)
        {
            SetNoIdeOptions_c("none");
            return;
        }

        UpdateIdeOptionsForSelection_c(_currentResults[SelectedIndex]);
    }

    private async Task LaunchSelectedAsync_c(int? alternativeIndex)
    {
        if (!IsProjectSearchMode)
        {
            return;
        }

        if (SelectedIndex < 0 || SelectedIndex >= _currentResults.Count)
        {
            return;
        }

        var selectedResult = _currentResults[SelectedIndex];
        var selectedTool = ResolveLaunchTool_c(selectedResult, alternativeIndex);
        if (selectedTool is null)
        {
            UpdateStatus_c("Alternative tool slot not available.");
            return;
        }

        if (_application.IsTextEditorTool(selectedTool.ToolId))
        {
            await EnterFilePickerModeAsync_c(selectedResult.Project, selectedTool);
            return;
        }

        var launchResult = _application.Launch(selectedResult, alternativeIndex);
        UpdateStatus_c(launchResult.Message);

        if (launchResult.IsSuccess)
        {
            RequestHideLauncher?.Invoke(this, EventArgs.Empty);
            await RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
        }
    }

    private Launcher.Core.Models.ToolRecord_c? ResolveLaunchTool_c(Launcher.Core.Models.SearchResult_c selectedResult, int? alternativeIndex)
    {
        if (!alternativeIndex.HasValue)
        {
            return selectedResult.SuggestedTool;
        }

        if (alternativeIndex.Value < 0 || alternativeIndex.Value >= selectedResult.AlternativeTools.Count)
        {
            return null;
        }

        return selectedResult.AlternativeTools[alternativeIndex.Value];
    }

    private async Task EnterFilePickerModeAsync_c(Launcher.Core.Models.ProjectRecord_c project, Launcher.Core.Models.ToolRecord_c tool)
    {
        _filePickerProject = project;
        _filePickerTool = tool;
        _filePickerEntries.Clear();
        _filePickerVisibleEntries.Clear();
        _filePickerReturnSearchText = SearchText;
        SetSearchTextWithoutEvent_c(string.Empty);
        CurrentMode = LauncherMode_c.FilePicker;
        SetNoIdeOptions_c("picker");
        UpdateStatus_c("Loading project files...");
        RequestFocusSearch?.Invoke(this, EventArgs.Empty);

        _filePickerLoadCts?.Cancel();
        _filePickerLoadCts?.Dispose();
        _filePickerLoadCts = new CancellationTokenSource();

        try
        {
            var files = await Task.Run(() => BuildFilePickerEntries_c(project.ProjectPath, _filePickerLoadCts.Token), _filePickerLoadCts.Token);
            _filePickerEntries.AddRange(files);
            RenderFilePickerRows_c();
        }
        catch (OperationCanceledException)
        {
            // Ignore stale load.
        }
        catch (Exception ex)
        {
            UpdateStatus_c($"File picker failed: {ex.Message}");
        }
    }

    private List<FilePickerEntry_c> BuildFilePickerEntries_c(string projectPath, CancellationToken cancellationToken)
    {
        var entries = new List<FilePickerEntry_c>();
        foreach (var file in Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(projectPath, file);
            entries.Add(new FilePickerEntry_c
            {
                FullPath = file,
                RelativePath = relativePath,
                DisplayName = Path.GetFileName(file)
            });

            if (entries.Count >= 5000)
            {
                break;
            }
        }

        return entries
            .OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<FilePickerEntry_c> BuildFilePickerProjection_c(string rawQuery)
    {
        var query = (rawQuery ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return _filePickerEntries
                .OrderByDescending(entry => ResolveFileTypePriority_c(entry.RelativePath))
                .ThenBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var scoredEntries = _filePickerEntries
            .Select(entry => new
            {
                Entry = entry,
                Score = ScoreFilePickerEntry_c(entry, query)
            })
            .Where(item => item.Score > int.MinValue)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Entry.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Entry)
            .ToList();
        return scoredEntries;
    }

    private static int ScoreFilePickerEntry_c(FilePickerEntry_c entry, string query)
    {
        var fileName = entry.DisplayName;
        var relativePath = entry.RelativePath;
        var score = 0;
        var isMatch = false;

        if (fileName.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 900;
            isMatch = true;
        }

        if (fileName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 650;
            isMatch = true;
        }
        else if (fileName.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 420;
            isMatch = true;
        }

        if (relativePath.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 240;
            isMatch = true;
        }
        else if (relativePath.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 120;
            isMatch = true;
        }

        var fileNameSubsequenceScore = ScoreSubsequenceMatch_c(fileName, query);
        if (fileNameSubsequenceScore >= 0)
        {
            score += 120 + fileNameSubsequenceScore;
            isMatch = true;
        }

        var pathSubsequenceScore = ScoreSubsequenceMatch_c(relativePath, query);
        if (pathSubsequenceScore >= 0)
        {
            score += 60 + pathSubsequenceScore;
            isMatch = true;
        }

        if (!isMatch)
        {
            return int.MinValue;
        }

        return score + (ResolveFileTypePriority_c(relativePath) * 100);
    }

    private static int ScoreSubsequenceMatch_c(string source, string query)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(query))
        {
            return -1;
        }

        var sourceSpan = source.AsSpan();
        var querySpan = query.AsSpan();
        var sourceIndex = 0;
        var firstMatchIndex = -1;
        var lastMatchIndex = -1;
        for (var queryIndex = 0; queryIndex < querySpan.Length; queryIndex++)
        {
            var nextMatchIndex = FindNextCharMatchIndex_c(sourceSpan, querySpan[queryIndex], sourceIndex);
            if (nextMatchIndex < 0)
            {
                return -1;
            }

            if (firstMatchIndex < 0)
            {
                firstMatchIndex = nextMatchIndex;
            }

            lastMatchIndex = nextMatchIndex;
            sourceIndex = nextMatchIndex + 1;
        }

        var spanLength = lastMatchIndex - firstMatchIndex + 1;
        var compactnessPenalty = Math.Max(0, spanLength - querySpan.Length);
        return Math.Max(0, (querySpan.Length * 4) - (compactnessPenalty * 2));
    }

    private static int FindNextCharMatchIndex_c(ReadOnlySpan<char> source, char target, int startIndex)
    {
        for (var index = startIndex; index < source.Length; index++)
        {
            if (char.ToUpperInvariant(source[index]) == char.ToUpperInvariant(target))
            {
                return index;
            }
        }

        return -1;
    }

    private static int ResolveFileTypePriority_c(string relativePath)
    {
        var extension = Path.GetExtension(relativePath);
        if (KnownTextFileExtensions_c.Contains(extension))
        {
            return 1;
        }

        if (KnownBinaryFileExtensions_c.Contains(extension))
        {
            return -1;
        }

        return 0;
    }

    private string? GetSelectedVisibleFilePickerPath_c()
    {
        if (SelectedIndex < 0 || SelectedIndex >= _filePickerVisibleEntries.Count)
        {
            return null;
        }

        return _filePickerVisibleEntries[SelectedIndex].FullPath;
    }

    private int FindVisibleFilePickerIndexByPath_c(string? fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return -1;
        }

        for (var index = 0; index < _filePickerVisibleEntries.Count; index++)
        {
            if (string.Equals(_filePickerVisibleEntries[index].FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private async Task LaunchSelectedFileFromPickerAsync_c()
    {
        if (!IsFilePickerMode || _filePickerProject is null || _filePickerTool is null)
        {
            return;
        }

        if (SelectedIndex < 0 || SelectedIndex >= _filePickerVisibleEntries.Count)
        {
            return;
        }

        var selected = _filePickerVisibleEntries[SelectedIndex];
        var launchResult = _application.LaunchFile(_filePickerProject, _filePickerTool, selected.FullPath);
        UpdateStatus_c(launchResult.Message);
        if (launchResult.IsSuccess)
        {
            RequestHideLauncher?.Invoke(this, EventArgs.Empty);
            ExitFilePickerMode_c();
            await RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
        }
    }

    private void ExitFilePickerMode_c()
    {
        _filePickerLoadCts?.Cancel();
        _filePickerLoadCts?.Dispose();
        _filePickerLoadCts = null;

        _filePickerEntries.Clear();
        _filePickerVisibleEntries.Clear();
        _filePickerProject = null;
        _filePickerTool = null;
        var returnSearchText = _filePickerReturnSearchText;
        _filePickerReturnSearchText = string.Empty;
        SetSearchTextWithoutEvent_c(returnSearchText);

        CurrentMode = LauncherMode_c.ProjectSearch;
        _ = RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
    }

    private async Task ExecuteMetaCommandAsync_c(string commandText)
    {
        var commandToRun = commandText;
        if (SelectedIndex >= 0 && SelectedIndex < _currentMetaSuggestions.Count)
        {
            commandToRun = _currentMetaSuggestions[SelectedIndex].Command;
        }

        var metaResult = _application.ExecutePowerCommand(commandToRun);
        UpdateStatus_c(metaResult.Message);
        if (metaResult.RequestAppExit)
        {
            RequestApplicationExit?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (metaResult.RequestOpenOnboarding)
        {
            await EnterOnboardingModeAsync_c();
            return;
        }

        if (metaResult.RequestOpenSettings)
        {
            OpenSettings();
            return;
        }

        await RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
    }

    private async Task ExecuteTerminalCommandAsync_c(string rawInput)
    {
        var trimmed = rawInput.TrimStart();
        var commandText = trimmed.Length > 1 ? trimmed[1..].Trim() : string.Empty;
        if (string.Equals(commandText, "/clear", StringComparison.OrdinalIgnoreCase))
        {
            await ClearTerminalOutputAsync();
            return;
        }

        var terminalResult = await _application.ExecuteTerminalCommandAsync(rawInput, CancellationToken.None);
        if (!terminalResult.IsSuccess)
        {
            UpdateStatus_c(terminalResult.ErrorMessage);
        }
        else
        {
            var exitPart = terminalResult.ExitCode.HasValue ? $" (exit {terminalResult.ExitCode.Value})" : string.Empty;
            UpdateStatus_c($"Terminal command executed{exitPart}.");
        }

        SetSearchTextWithoutEvent_c(">");
        RequestFocusSearch?.Invoke(this, EventArgs.Empty);
        await RenderTerminalProjectionAsync_c(CancellationToken.None);
    }

    public async Task ClearTerminalOutputAsync()
    {
        _application.ClearTerminalOutput();
        SetSearchTextWithoutEvent_c(">");
        RequestFocusSearch?.Invoke(this, EventArgs.Empty);
        UpdateStatus_c("Cleared.");
        await RenderTerminalProjectionAsync_c(CancellationToken.None);
    }

    private async Task SendAiPromptAsync_c()
    {
        if (_isAiRequestInFlight || !IsAiChatSetUp_c())
        {
            RaiseAiBusyEnterFeedbackRequested_c();
            return;
        }

        var prompt = NormalizeAiPrompt_c(SearchText);
        if (string.IsNullOrWhiteSpace(prompt))
        {
            UpdateStatus_c("AI prompt is empty.");
            return;
        }

        SetAiRequestInFlight_c(true);
        UpdateStatus_c("Sending AI prompt...");
        ResetAiOutputOnly_c();
        try
        {
            var result = await _application.ExecuteAiChatAsync(prompt, CancellationToken.None);

            if (result.IsSuccess)
            {
                AiMarkdownBuilder.Append(string.IsNullOrWhiteSpace(result.OutputMarkdown)
                    ? "(empty response)"
                    : result.OutputMarkdown);
                UpdateStatus_c("AI response updated.");
            }
            else
            {
                AiMarkdownBuilder.Append($"### AI error\n\n{result.ErrorMessage}");
                var errorStatusLine = result.ErrorMessage?
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .LastOrDefault() ?? "AI error.";
            }
        }
        catch (Exception ex)
        {
            AiMarkdownBuilder.Clear();
            AiMarkdownBuilder.Append($"### AI error\n\n{ex.Message}");
        }
        finally
        {
            SetAiRequestInFlight_c(false);
            SetSearchTextWithoutEvent_c("?");
            RequestFocusSearch?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SetAiRequestInFlight_c(bool value)
    {
        if (_isAiRequestInFlight == value)
        {
            return;
        }

        _isAiRequestInFlight = value;
        OnPropertyChanged_c(nameof(IsAiResponseBlocked));
        if (IsAiChatMode)
        {
            StatusText = _isAiRequestInFlight ? "Sending AI prompt..." : "NOTE: This is a stateless AI chat session";
        }
    }

    private void RaiseAiBusyEnterFeedbackRequested_c()
    {
        AiBusyEnterFeedbackRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ResetAiSessionState_c(bool clearPromptInput)
    {
        ResetAiOutputOnly_c();
        if (clearPromptInput)
        {
            SetSearchTextWithoutEvent_c("?");
        }
    }

    private void ResetAiOutputOnly_c()
    {
        AiMarkdownBuilder.Clear();
    }

    private void UpdateStatus_c(string text)
    {
        var message = (text ?? string.Empty).Trim();
        if (message.Length == 0)
        {
            StatusText = string.Empty;
            return;
        }

        if (HasStatusPrefix_c(message))
        {
            StatusText = message;
            return;
        }

        StatusText = $"{ResolveStatusPrefix_c()}: {message}";
    }

    private void OnFooterVisibilityStateChanged_c()
    {
        OnPropertyChanged_c(nameof(IsFooterVisible));
        OnPropertyChanged_c(nameof(IsFooterMetaHintVisible));
        OnPropertyChanged_c(nameof(IsFooterIdeOptionsVisible));
        OnPropertyChanged_c(nameof(IsFooterTerminalHintVisible));
        OnPropertyChanged_c(nameof(IsFooterAiHintVisible));
        OnPropertyChanged_c(nameof(IsFooterFilePickerHintVisible));
        OnPropertyChanged_c(nameof(IsFooterStatusVisible));
        OnPropertyChanged_c(nameof(IsIdeOptionsVisible));
    }

    private string ResolveStatusPrefix_c()
    {
        if (IsOnboardingVisible)
        {
            return "Onboarding";
        }

        if (IsSettingsVisible)
        {
            return "Settings";
        }

        if (IsAiChatMode)
        {
            return "AI";
        }

        if (IsTerminalMode)
        {
            return "Terminal";
        }

        if (IsFilePickerMode)
        {
            return "File Picker";
        }

        return "Search";
    }

    private static bool HasStatusPrefix_c(string message)
    {
        return message.StartsWith("Search:", StringComparison.OrdinalIgnoreCase) ||
               message.StartsWith("Terminal:", StringComparison.OrdinalIgnoreCase) ||
               message.StartsWith("AI:", StringComparison.OrdinalIgnoreCase) ||
               message.StartsWith("Settings:", StringComparison.OrdinalIgnoreCase) ||
               message.StartsWith("Onboarding:", StringComparison.OrdinalIgnoreCase) ||
               message.StartsWith("File Picker:", StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshDynamicUiText_c()
    {
        CommandHintText = CurrentMode switch
        {
            LauncherMode_c.AiChat => "ai | Enter send | Shift+Enter newline",
            LauncherMode_c.Terminal => "terminal | > command | ! meta",
            LauncherMode_c.FilePicker => "file picker | choose the specific file to open",
            _ => "projects | ? ai | > command | ! meta"
        };

        SearchWatermarkText = CurrentMode switch
        {
            LauncherMode_c.AiChat => "Ask AI (Shift+Enter inserts newline)",
            LauncherMode_c.Terminal => "> Type command and press Enter",
            LauncherMode_c.FilePicker => "Filter files by name/path. Text files rank first.",
            _ => "Search projects, ? ai, > command, or ! meta command"
        };

        SearchPrefixText = IsMetaCommandMode
            ? "!"
            : CurrentMode switch
            {
                LauncherMode_c.AiChat => "?",
                LauncherMode_c.FilePicker => "F",
                _ => ">"
            };

        AlternativeLaunchModifierLabelText = _application.Settings.GeneralHotkeys.AlternativeLaunchModifiers;
    }

    private void LoadSettingsDraftFromRuntime_c()
    {
        var general = _application.Settings.GeneralHotkeys;
        var terminal = _application.Settings.Terminal;
        var aiChat = _application.Settings.AiChat;

        SettingsGeneralExitText = general.Exit;
        SettingsGeneralMoveUpText = general.MoveUp;
        SettingsGeneralMoveDownText = general.MoveDown;
        SettingsGeneralConfirmText = general.Confirm;
        SettingsGeneralSwitchModeText = general.SwitchMode;
        SettingsGeneralAlternativeModifiersText = general.AlternativeLaunchModifiers;
        SettingsGeneralAlternativeKeysText = string.Join(", ", general.AlternativeLaunchKeys);
        SettingsThemeOverrideText = _application.Settings.ThemeOverride;

        SettingsTerminalShellExecutableText = terminal.ShellExecutable;
        SettingsTerminalShellArgumentsPrefixText = string.Join(" ", terminal.ShellArgumentsPrefix.Select(QuoteTokenIfNeeded_c));

        SettingsAiChatCliExecutableText = aiChat.CliExecutable;
        SettingsAiChatArgumentTemplateText = aiChat.ArgumentTemplate;
        SettingsAiChatContextDirectoryText = aiChat.ContextDirectory;
        SettingsAiChatTimeoutSecondsText = aiChat.TimeoutSeconds.ToString();

        var behavior = _application.Settings.Behavior;
        SettingsBehaviorPersistModeOnLaunch = behavior.PersistModeOnLaunch;
        SettingsBehaviorPersistProjectSearchState = behavior.PersistProjectSearchState;
        SettingsBehaviorPersistAiChatState = behavior.PersistAiChatState;
        SettingsBehaviorPersistTerminalState = behavior.PersistTerminalState;
        RefreshDynamicUiText_c();
    }

    private static bool TryNormalizeThemeOverride_c(string? raw, out string normalized)
    {
        if (string.Equals(raw, Launcher.Core.Models.ThemeOverrideOptions_c.Light, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Launcher.Core.Models.ThemeOverrideOptions_c.Light;
            return true;
        }

        if (string.Equals(raw, Launcher.Core.Models.ThemeOverrideOptions_c.Dark, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Launcher.Core.Models.ThemeOverrideOptions_c.Dark;
            return true;
        }

        if (string.Equals(raw, Launcher.Core.Models.ThemeOverrideOptions_c.Default, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Launcher.Core.Models.ThemeOverrideOptions_c.Default;
            return true;
        }

        normalized = Launcher.Core.Models.ThemeOverrideOptions_c.Default;
        return false;
    }

    private static void ApplyThemeOverride_c(string themeOverride)
    {
        var app = Avalonia.Application.Current;
        if (app is null)
        {
            return;
        }

        app.RequestedThemeVariant = ResolveThemeVariant_c(themeOverride);
    }

    private static ThemeVariant ResolveThemeVariant_c(string themeOverride)
    {
        if (string.Equals(themeOverride, Launcher.Core.Models.ThemeOverrideOptions_c.Light, StringComparison.OrdinalIgnoreCase))
        {
            return ThemeVariant.Light;
        }

        if (string.Equals(themeOverride, Launcher.Core.Models.ThemeOverrideOptions_c.Dark, StringComparison.OrdinalIgnoreCase))
        {
            return ThemeVariant.Dark;
        }

        return ThemeVariant.Default;
    }

    private bool TryBuildGeneralHotkeysFromSettingsDraft_c(
        out Launcher.Core.Models.GeneralHotkeySettings_c settings,
        out string? error)
    {
        settings = Launcher.Core.Models.GeneralHotkeySettings_c.CreateDefault();
        error = null;

        if (!TryNormalizeChord_c(SettingsGeneralExitText, out var exit))
        {
            error = "General hotkey Exit is invalid.";
            return false;
        }

        if (!TryNormalizeChord_c(SettingsGeneralMoveUpText, out var moveUp))
        {
            error = "General hotkey MoveUp is invalid.";
            return false;
        }

        if (!TryNormalizeChord_c(SettingsGeneralMoveDownText, out var moveDown))
        {
            error = "General hotkey MoveDown is invalid.";
            return false;
        }

        if (!TryNormalizeChord_c(SettingsGeneralConfirmText, out var confirm))
        {
            error = "General hotkey Confirm is invalid.";
            return false;
        }

        if (!TryNormalizeChord_c(SettingsGeneralSwitchModeText, out var switchMode))
        {
            error = "General hotkey SwitchMode is invalid.";
            return false;
        }

        if (!TryNormalizeModifierOnlyChord_c(SettingsGeneralAlternativeModifiersText, out var alternativeModifiers))
        {
            error = "General hotkey AlternativeLaunchModifiers is invalid.";
            return false;
        }

        var alternativeKeys = SettingsGeneralAlternativeKeysText
            .Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (alternativeKeys.Count == 0)
        {
            error = "General hotkey AlternativeLaunchKeys is empty.";
            return false;
        }

        var normalizedAlternativeKeys = new List<string>();
        foreach (var keyToken in alternativeKeys)
        {
            if (!TryNormalizeKeyToken_c(keyToken, out var normalizedKey))
            {
                error = $"Alternative launch key is invalid: {keyToken}";
                return false;
            }

            normalizedAlternativeKeys.Add(normalizedKey);
        }

        settings = new Launcher.Core.Models.GeneralHotkeySettings_c
        {
            Exit = exit,
            MoveUp = moveUp,
            MoveDown = moveDown,
            Confirm = confirm,
            SwitchMode = switchMode,
            AlternativeLaunchModifiers = alternativeModifiers,
            AlternativeLaunchKeys = normalizedAlternativeKeys
        };

        if (HasGeneralHotkeyConflicts_c(settings, _application.Settings.Hotkeys.Toggle))
        {
            error = "General hotkeys conflict with each other or with Open launcher hotkey.";
            return false;
        }

        return true;
    }

    private static bool HasGeneralHotkeyConflicts_c(Launcher.Core.Models.GeneralHotkeySettings_c settings, string toggleHotkey)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (TryNormalizeChord_c(toggleHotkey, out var normalizedToggle))
        {
            seen.Add(normalizedToggle);
        }

        if (!seen.Add(settings.Exit))
        {
            return true;
        }

        if (!seen.Add(settings.MoveUp))
        {
            return true;
        }

        if (!seen.Add(settings.MoveDown))
        {
            return true;
        }

        if (!seen.Add(settings.Confirm))
        {
            return true;
        }

        if (!seen.Add(settings.SwitchMode))
        {
            return true;
        }

        foreach (var key in settings.AlternativeLaunchKeys)
        {
            if (!TryNormalizeChord_c($"{settings.AlternativeLaunchModifiers}+{key}", out var normalizedAlt))
            {
                return true;
            }

            if (!seen.Add(normalizedAlt))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryNormalizeChord_c(string raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var parts = raw
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        if (parts.Count == 0)
        {
            return false;
        }

        var modifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? key = null;
        foreach (var part in parts)
        {
            if (TryNormalizeModifierToken_c(part, out var modifier))
            {
                modifiers.Add(modifier);
                continue;
            }

            if (key is not null || !TryNormalizeKeyToken_c(part, out var normalizedKey))
            {
                return false;
            }

            key = normalizedKey;
        }

        if (key is null)
        {
            return false;
        }

        normalized = BuildCanonicalChord_c(modifiers, key);
        return true;
    }

    private static bool TryNormalizeModifierOnlyChord_c(string raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var parts = raw
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        if (parts.Count == 0)
        {
            return false;
        }

        var modifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            if (!TryNormalizeModifierToken_c(part, out var modifier))
            {
                return false;
            }

            modifiers.Add(modifier);
        }

        normalized = BuildCanonicalChord_c(modifiers, key: null);
        return true;
    }

    private static bool TryNormalizeKeyToken_c(string raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (Enum.TryParse<Key>(raw, ignoreCase: true, out var parsed))
        {
            normalized = parsed.ToString();
            return true;
        }

        if (raw.Length == 1 && char.IsLetter(raw[0]) &&
            Enum.TryParse<Key>(raw.ToUpperInvariant(), ignoreCase: true, out parsed))
        {
            normalized = parsed.ToString();
            return true;
        }

        return false;
    }

    private static bool TryNormalizeModifierToken_c(string raw, out string modifier)
    {
        modifier = string.Empty;
        if (string.Equals(raw, "Ctrl", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "Control", StringComparison.OrdinalIgnoreCase))
        {
            modifier = "Ctrl";
            return true;
        }

        if (string.Equals(raw, "Alt", StringComparison.OrdinalIgnoreCase))
        {
            modifier = "Alt";
            return true;
        }

        if (string.Equals(raw, "Shift", StringComparison.OrdinalIgnoreCase))
        {
            modifier = "Shift";
            return true;
        }

        if (string.Equals(raw, "Win", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            modifier = "Win";
            return true;
        }

        return false;
    }

    private static string BuildCanonicalChord_c(HashSet<string> modifiers, string? key)
    {
        var ordered = new List<string>();
        if (modifiers.Contains("Ctrl"))
        {
            ordered.Add("Ctrl");
        }

        if (modifiers.Contains("Alt"))
        {
            ordered.Add("Alt");
        }

        if (modifiers.Contains("Shift"))
        {
            ordered.Add("Shift");
        }

        if (modifiers.Contains("Win"))
        {
            ordered.Add("Win");
        }

        if (!string.IsNullOrWhiteSpace(key))
        {
            ordered.Add(key);
        }

        return string.Join("+", ordered);
    }

    private static List<string> TokenizeCommandText_c(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        char quote = '\0';

        foreach (var ch in raw)
        {
            if (quote == '\0' && char.IsWhiteSpace(ch))
            {
                FlushCommandToken_c(tokens, current);
                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                if (quote == '\0')
                {
                    quote = ch;
                    continue;
                }

                if (quote == ch)
                {
                    quote = '\0';
                    continue;
                }
            }

            current.Append(ch);
        }

        FlushCommandToken_c(tokens, current);
        return tokens;
    }

    private static void FlushCommandToken_c(List<string> tokens, System.Text.StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        tokens.Add(current.ToString());
        current.Clear();
    }

    private static string QuoteTokenIfNeeded_c(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !raw.Any(char.IsWhiteSpace))
        {
            return raw;
        }

        return $"\"{raw}\"";
    }

    private void SetSettingsError_c(string message)
    {
        SettingsErrorText = message;
        HasSettingsError = true;
        UpdateStatus_c(message);
    }

    private void ClearSettingsError_c()
    {
        SettingsErrorText = string.Empty;
        HasSettingsError = false;
    }

    private async Task EnterOnboardingModeAsync_c()
    {
        IsSettingsVisible = false;
        IsOnboardingVisible = true;
        _application.MarkOnboardingSeen();

        ConfigureOnboardingRootSuggestions_c();
        ClearOnboardingError_c();

        OnboardingHotkeyText = string.IsNullOrWhiteSpace(_application.Settings.Hotkeys.Toggle)
            ? "Alt+Shift+Space"
            : _application.Settings.Hotkeys.Toggle;

        RequestFocusOnboardingHotkey?.Invoke(this, EventArgs.Empty);
        await RefreshOnboardingToolCheckAsync_c();
    }

    private void ConfigureOnboardingRootSuggestions_c()
    {
        OnboardingRoots.Clear();
        OnboardingAddRootText = string.Empty;

        var existingRoots = _application.Settings.Roots;
        if (existingRoots.Count > 0)
        {
            foreach (var r in existingRoots)
                OnboardingRoots.Add(new OnboardingRootItem_c { Path = r.Path, Enabled = r.Enabled });
            return;
        }

        foreach (var s in _application.BuildSuggestedOnboardingRoots())
            OnboardingRoots.Add(new OnboardingRootItem_c { Path = s, Enabled = true });
    }

    private async Task RefreshOnboardingToolCheckAsync_c()
    {
        OnboardingToolCheckStatusText = "Checking installed tools...";
        OnboardingFinishEnabled = false;

        try
        {
            var detected = await _application.RefreshDetectedToolsAsync(CancellationToken.None);
            _onboardingDetectedTools.Clear();
            _onboardingDetectedTools.AddRange(detected.Where(x => x.IsAvailable));

            OnboardingToolRows.Clear();
            foreach (var row in _onboardingDetectedTools.Select(x => $"{x.DisplayName} | {x.ExecutablePath}"))
            {
                OnboardingToolRows.Add(row);
            }

            OnboardingDetectedTools.Clear();
            foreach (var tool in _onboardingDetectedTools)
            {
                OnboardingDetectedTools.Add(tool);
            }

            var preferredToolId = _application.Settings.Defaults.FallbackToolId;
            SelectedPreferredTool = _onboardingDetectedTools
                .FirstOrDefault(x => string.Equals(x.ToolId, preferredToolId, StringComparison.OrdinalIgnoreCase))
                ?? _onboardingDetectedTools.FirstOrDefault();

            OnboardingToolCheckStatusText = _onboardingDetectedTools.Count == 0
                ? "No known editor/IDE detected. You can still finish."
                : $"{_onboardingDetectedTools.Count} tool(s) detected.";
        }
        catch (Exception ex)
        {
            OnboardingToolCheckStatusText = $"Tool check failed: {ex.Message}";
            OnboardingToolRows.Clear();
            OnboardingToolRows.Add("Tool check failed. Continue with manual settings later.");
            OnboardingDetectedTools.Clear();
            SelectedPreferredTool = null;
        }
        finally
        {
            OnboardingFinishEnabled = true;
        }
    }

    private void ExitOnboardingMode_c()
    {
        IsOnboardingVisible = false;
        ClearOnboardingError_c();
        CurrentMode = LauncherMode_c.ProjectSearch;
        _ = RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
        RequestFocusSearch?.Invoke(this, EventArgs.Empty);
    }

    private List<string> CollectOnboardingRoots_c()
    {
        var selected = OnboardingRoots.Where(r => r.Enabled).Select(r => r.Path).ToList();
        foreach (var path in selected.Where(p => !Directory.Exists(p)))
        {
            SetOnboardingError_c($"Root missing: {path}");
            return [];
        }
        return selected.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void SetOnboardingError_c(string message)
    {
        OnboardingErrorText = message;
        HasOnboardingError = true;
    }

    private void ClearOnboardingError_c()
    {
        OnboardingErrorText = string.Empty;
        HasOnboardingError = false;
    }

    private void EmitHotkeyUpdateIfChanged_c(string previousHotkey)
    {
        if (string.Equals(previousHotkey, _application.Settings.Hotkeys.Toggle, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        HotkeyUpdated?.Invoke(this, _application.Settings.Hotkeys.Toggle);
    }

    private IdeOptionItem_c BuildDefaultIdeOption_c(Launcher.Core.Models.SearchResult_c selectedResult)
    {
        return new IdeOptionItem_c
        {
            SlotLabel = "Enter",
            ToolName = selectedResult.SuggestedTool.DisplayName,
            LaunchToken = "default",
            IsEnabled = true,
            ToolIcon = _toolIconResolver.Resolve(selectedResult.SuggestedTool)
        };
    }

    private List<IdeOptionItem_c> BuildAlternateIdeOptions_c(Launcher.Core.Models.SearchResult_c selectedResult)
    {
        var keys = _application.Settings.GeneralHotkeys.AlternativeLaunchKeys;
        return selectedResult.AlternativeTools
            .Take(keys.Count)
            .Select((tool, i) => new IdeOptionItem_c
            {
                SlotLabel = keys[i],
                ToolName = tool.DisplayName,
                LaunchToken = $"alt:{i}",
                IsEnabled = true,
                ToolIcon = _toolIconResolver.Resolve(tool)
            })
            .ToList();
    }

    private void UpdateIdeOptionsForSelection_c(Launcher.Core.Models.SearchResult_c selectedResult)
    {
        DefaultIdeOptionItems.Clear();
        DefaultIdeOptionItems.Add(BuildDefaultIdeOption_c(selectedResult));

        AlternateIdeOptionItems.Clear();
        var alternates = BuildAlternateIdeOptions_c(selectedResult);
        if (alternates.Count == 0)
        {
            AlternateIdeOptionItems.Add(new IdeOptionItem_c
            {
                SlotLabel = "none",
                ToolName = "none",
                LaunchToken = string.Empty,
                IsEnabled = false,
                ToolIcon = _toolIconResolver.ResolveNeutral()
            });
            return;
        }

        foreach (var item in alternates)
        {
            AlternateIdeOptionItems.Add(item);
        }
    }

    private void SetNoIdeOptions_c(string text)
    {
        DefaultIdeOptionItems.Clear();
        DefaultIdeOptionItems.Add(new IdeOptionItem_c
        {
            SlotLabel = "Enter",
            ToolName = text,
            LaunchToken = string.Empty,
            IsEnabled = false,
            ToolIcon = _toolIconResolver.ResolveNeutral()
        });

        AlternateIdeOptionItems.Clear();
        AlternateIdeOptionItems.Add(new IdeOptionItem_c
        {
            SlotLabel = "none",
            ToolName = text,
            LaunchToken = string.Empty,
            IsEnabled = false,
            ToolIcon = _toolIconResolver.ResolveNeutral()
        });
    }

    private string? GetSelectedProjectPath_c()
    {
        if (SelectedIndex < 0 || SelectedIndex >= _currentResults.Count)
        {
            return null;
        }

        return _currentResults[SelectedIndex].Project.ProjectPath;
    }

    private int FindProjectIndexByPath_c(string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return -1;
        }

        for (var i = 0; i < _currentResults.Count; i++)
        {
            if (string.Equals(_currentResults[i].Project.ProjectPath, projectPath, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private string? GetSelectedMetaCommand_c()
    {
        if (SelectedIndex < 0 || SelectedIndex >= _currentMetaSuggestions.Count)
        {
            return null;
        }

        return _currentMetaSuggestions[SelectedIndex].Command;
    }

    private int FindMetaIndexByCommand_c(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return -1;
        }

        for (var i = 0; i < _currentMetaSuggestions.Count; i++)
        {
            if (string.Equals(_currentMetaSuggestions[i].Command, command, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private int GetListItemCount_c()
    {
        if (_currentMetaSuggestions.Count > 0)
        {
            return _currentMetaSuggestions.Count;
        }

        if (IsFilePickerMode)
        {
            return _filePickerVisibleEntries.Count == 0 ? 1 : _filePickerVisibleEntries.Count;
        }

        if (IsTerminalMode)
        {
            return DisplayRows.Count;
        }

        return _currentResults.Count;
    }

    private bool IsMetaCycleActive_c()
    {
        return IsMetaCommandMode || SearchText.TrimStart().StartsWith("!", StringComparison.Ordinal);
    }

    private bool IsAiChatSetUp_c()
    {
        return !string.IsNullOrWhiteSpace(_application.Settings.AiChat.CliExecutable);
    }

    private void EnterMetaCycleMode_c()
    {
        CancelScheduledProjectionRefresh_c();
        CurrentMode = LauncherMode_c.ProjectSearch;
        SetSearchTextWithoutEvent_c("!");
        IsMetaCommandMode = true;
        _ = RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
    }

    private void ExitMetaCycleMode_c()
    {
        CancelScheduledProjectionRefresh_c();
        SetSearchTextWithoutEvent_c(string.Empty);
        IsMetaCommandMode = false;
        _ = RefreshProjectionForCurrentModeAsync_c(CancellationToken.None);
    }

    private static string TrimAiPrefix_c(string trimmedInput)
    {
        if (string.IsNullOrEmpty(trimmedInput) || !trimmedInput.StartsWith('?'))
        {
            return trimmedInput;
        }

        return trimmedInput.Length == 1
            ? string.Empty
            : trimmedInput[1..].TrimStart();
    }

    private static string NormalizeAiPrompt_c(string rawInput)
    {
        var trimmed = (rawInput ?? string.Empty).Trim();
        return TrimAiPrefix_c(trimmed);
    }

    private void SetSearchTextWithoutEvent_c(string value)
    {
        _suppressSearchTextChange = true;
        SearchText = value;
        _suppressSearchTextChange = false;
    }

    private void SaveAiStateToTempFile_c()
    {
        try
        {
            var content = AiMarkdownBuilder.ToString();
            if (!string.IsNullOrEmpty(content))
                File.WriteAllText(AiStateTempPath, content);
        }
        catch { /* best-effort */ }
    }

    private void RestoreAiStateFromTempFile_c()
    {
        try
        {
            if (!File.Exists(AiStateTempPath)) return;
            var content = File.ReadAllText(AiStateTempPath);
            if (!string.IsNullOrEmpty(content))
                AiMarkdownBuilder.Append(content);
        }
        catch { /* best-effort */ }
    }

    private void SaveTerminalStateToTempFile_c()
    {
        try
        {
            var lines = _application.GetTerminalOutputSnapshot();
            if (lines.Count > 0)
                File.WriteAllLines(TerminalStateTempPath, lines);
        }
        catch { /* best-effort */ }
    }

    private void RestoreTerminalStateFromTempFile_c()
    {
        try
        {
            if (!File.Exists(TerminalStateTempPath)) return;
            var lines = File.ReadAllLines(TerminalStateTempPath);
            if (lines.Length > 0)
                _application.RestoreTerminalOutput(lines);
        }
        catch { /* best-effort */ }
    }
}
