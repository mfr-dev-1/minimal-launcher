namespace Launcher.Core.Models;

public sealed class LauncherSettings_c
{
    public int Version { get; set; } = 1;

    public string ThemeOverride { get; set; } = ThemeOverrideOptions_c.Default;

    public HotkeySettings_c Hotkeys { get; set; } = new();

    public List<RootPathSetting_c> Roots { get; set; } = [];

    public List<ToolPathOverride_c> ToolPaths { get; set; } = [];

    public List<ProjectToolOverride_c> ProjectOverrides { get; set; } = [];

    public List<ProjectLastUsedTool_c> LastUsedProjectTools { get; set; } = [];

    public IndexingSettings_c Indexing { get; set; } = IndexingSettings_c.CreateDefault();

    public SearchSettings_c Search { get; set; } = new();

    public DefaultSettings_c Defaults { get; set; } = DefaultSettings_c.CreateDefault();

    public GeneralHotkeySettings_c GeneralHotkeys { get; set; } = GeneralHotkeySettings_c.CreateDefault();

    public TerminalSettings_c Terminal { get; set; } = TerminalSettings_c.CreateDefault();

    public AiChatSettings_c AiChat { get; set; } = AiChatSettings_c.CreateDefault();

    public OnboardingSettings_c Onboarding { get; set; } = OnboardingSettings_c.CreatePending();

    public BehaviorSettings_c Behavior { get; set; } = BehaviorSettings_c.CreateDefault();

    public static LauncherSettings_c CreateDefault()
    {
        return new LauncherSettings_c
        {
            Version = 1,
            ThemeOverride = ThemeOverrideOptions_c.Default,
            Hotkeys = new HotkeySettings_c { Toggle = "Alt+Shift+Space" },
            Roots = [],
            ToolPaths = [],
            ProjectOverrides = [],
            LastUsedProjectTools = [],
            Indexing = IndexingSettings_c.CreateDefault(),
            Search = new SearchSettings_c { DebounceMs = 120, MaxResults = 50 },
            Defaults = DefaultSettings_c.CreateDefault(),
            GeneralHotkeys = GeneralHotkeySettings_c.CreateDefault(),
            Terminal = TerminalSettings_c.CreateDefault(),
            AiChat = AiChatSettings_c.CreateDefault(),
            Onboarding = OnboardingSettings_c.CreatePending(),
            Behavior = BehaviorSettings_c.CreateDefault()
        };
    }
}

public static class ThemeOverrideOptions_c
{
    public const string Default = "Default";
    public const string Light = "Light";
    public const string Dark = "Dark";
}

public sealed class HotkeySettings_c
{
    public string Toggle { get; set; } = "Alt+Shift+Space";
}

public sealed class RootPathSetting_c
{
    public string Path { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}

public sealed class ToolPathOverride_c
{
    public string ToolId { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;
}

public sealed class ProjectToolOverride_c
{
    public string ProjectPath { get; set; } = string.Empty;

    public string ToolId { get; set; } = string.Empty;
}

public sealed class ProjectLastUsedTool_c
{
    public string ProjectPath { get; set; } = string.Empty;

    public string ToolId { get; set; } = string.Empty;

    public DateTime LastUsedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class IndexingSettings_c
{
    public int MaxDepth { get; set; } = 20;

    public bool FollowSymlinks { get; set; }

    public bool IncludeHidden { get; set; }

    public List<string> ExcludeDirs { get; set; } = [];

    public static IndexingSettings_c CreateDefault()
    {
        return new IndexingSettings_c
        {
            MaxDepth = 20,
            FollowSymlinks = false,
            IncludeHidden = false,
            ExcludeDirs =
            [
                "node_modules", ".git", "bin", "obj", "dist", "build", "target", ".venv", "venv", ".next", ".nuxt", ".cache"
            ]
        };
    }
}

public sealed class SearchSettings_c
{
    public int DebounceMs { get; set; } = 120;

    public int MaxResults { get; set; } = 50;
}

public sealed class GeneralHotkeySettings_c
{
    public string Exit { get; set; } = "Escape";

    public string MoveUp { get; set; } = "Up";

    public string MoveDown { get; set; } = "Down";

    public string Confirm { get; set; } = "Enter";

    public string SwitchMode { get; set; } = "Shift+Tab";

    public string AlternativeLaunchModifiers { get; set; } = "Shift+Alt";

    public List<string> AlternativeLaunchKeys { get; set; } = BuildDefaultAlternativeLaunchKeys_c();

    public static GeneralHotkeySettings_c CreateDefault()
    {
        return new GeneralHotkeySettings_c
        {
            Exit = "Escape",
            MoveUp = "Up",
            MoveDown = "Down",
            Confirm = "Enter",
            SwitchMode = "Shift+Tab",
            AlternativeLaunchModifiers = "Shift+Alt",
            AlternativeLaunchKeys = BuildDefaultAlternativeLaunchKeys_c()
        };
    }

    public static List<string> BuildDefaultAlternativeLaunchKeys_c()
    {
        return ["Z", "X", "C", "V", "B", "N", "M"];
    }
}

public sealed class TerminalSettings_c
{
    public string ShellExecutable { get; set; } = "powershell.exe";

    public List<string> ShellArgumentsPrefix { get; set; } = BuildDefaultShellArgumentPrefix_c();

    public static TerminalSettings_c CreateDefault()
    {
        return new TerminalSettings_c
        {
            ShellExecutable = "powershell.exe",
            ShellArgumentsPrefix = BuildDefaultShellArgumentPrefix_c()
        };
    }

    public static List<string> BuildDefaultShellArgumentPrefix_c()
    {
        return ["-NoLogo", "-NoProfile", "-Command"];
    }
}

public sealed class AiChatSettings_c
{
    public string CliExecutable { get; set; } = string.Empty;

    public string ArgumentTemplate { get; set; } = "--prompt {prompt}";

    public string ContextDirectory { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 45;

    public static AiChatSettings_c CreateDefault()
    {
        return new AiChatSettings_c
        {
            CliExecutable = string.Empty,
            ArgumentTemplate = "--prompt {prompt}",
            ContextDirectory = string.Empty,
            TimeoutSeconds = 45
        };
    }
}

public sealed class DefaultSettings_c
{
    public string FallbackToolId { get; set; } = ToolIds_c.VsCode;

    public List<string> ToolPriorityOrder { get; set; } = BuildDefaultToolPriorityOrder_c();

    public static DefaultSettings_c CreateDefault()
    {
        return new DefaultSettings_c
        {
            FallbackToolId = ToolIds_c.VsCode,
            ToolPriorityOrder = BuildDefaultToolPriorityOrder_c()
        };
    }

    public static List<string> BuildDefaultToolPriorityOrder_c()
    {
        return
        [
            ToolIds_c.VisualStudio,
            ToolIds_c.Rider,
            ToolIds_c.VsCode,
            ToolIds_c.IntelliJ,
            ToolIds_c.PyCharm,
            ToolIds_c.WebStorm,
            ToolIds_c.GoLand,
            ToolIds_c.RustRover,
            ToolIds_c.CLion,
            ToolIds_c.PhpStorm,
            ToolIds_c.AndroidStudio,
            ToolIds_c.Cursor,
            ToolIds_c.Windsurf,
            ToolIds_c.SublimeText,
            ToolIds_c.NotepadPlusPlus,
            ToolIds_c.Vim,
            ToolIds_c.NeoVim,
            ToolIds_c.Eclipse
        ];
    }
}

public static class OnboardingStates_c
{
    public const string Pending = "pending";
    public const string Defaulted = "defaulted";
    public const string Skipped = "skipped";
    public const string Completed = "completed";
}

public sealed class OnboardingSettings_c
{
    public const int CurrentVersion_c = 1;

    public int Version { get; set; } = CurrentVersion_c;

    public string State { get; set; } = OnboardingStates_c.Pending;

    public DateTime? LastSeenUtc { get; set; }

    public static OnboardingSettings_c CreatePending()
    {
        return new OnboardingSettings_c
        {
            Version = CurrentVersion_c,
            State = OnboardingStates_c.Pending
        };
    }
}

public sealed class BehaviorSettings_c
{
    public bool PersistModeOnLaunch { get; set; }
    public bool PersistProjectSearchState { get; set; }
    public bool PersistAiChatState { get; set; }
    public bool PersistTerminalState { get; set; }

    public static BehaviorSettings_c CreateDefault() => new();
}
