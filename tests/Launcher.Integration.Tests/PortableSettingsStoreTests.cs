using Launcher.Core.Models;
using Launcher.Infrastructure.Storage;

namespace Launcher.Integration.Tests;

public sealed class PortableSettingsStore_c_Tests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "launcher-settings-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void LoadOrCreate_CreatesDefaultsAndPersistsRoundTrip()
    {
        Directory.CreateDirectory(_tempRoot);
        var store = new PortableSettingsStore_c(_tempRoot);

        var loaded = store.LoadOrCreate();
        loaded.Roots.Add(new RootPathSetting_c { Path = @"D:\src", Enabled = true });
        loaded.LastUsedProjectTools.Add(new ProjectLastUsedTool_c
        {
            ProjectPath = @"D:\src\alpha",
            ToolId = ToolIds_c.Rider,
            LastUsedUtc = DateTime.UtcNow
        });
        store.Save(loaded);

        var reloaded = store.LoadOrCreate();

        Assert.Equal("Alt+Shift+Space", reloaded.Hotkeys.Toggle);
        Assert.Contains(reloaded.Roots, x => x.Path == @"D:\src");
        Assert.Contains(reloaded.LastUsedProjectTools, x => x.ProjectPath == @"D:\src\alpha" && x.ToolId == ToolIds_c.Rider);
        Assert.Equal(OnboardingStates_c.Pending, reloaded.Onboarding.State);
        Assert.NotEmpty(reloaded.Defaults.ToolPriorityOrder);
    }

    [Fact]
    public void LoadOrCreate_BackfillsLastUsedProjectToolsWhenMissingInJson()
    {
        Directory.CreateDirectory(_tempRoot);
        var settingsPath = Path.Combine(_tempRoot, "launcher.settings.json");
        File.WriteAllText(settingsPath, """
{
  "version": 1,
  "hotkeys": { "toggle": "Alt+Shift+Space" },
  "roots": [],
  "toolPaths": [],
  "projectOverrides": [],
  "indexing": {
    "maxDepth": 20,
    "followSymlinks": false,
    "includeHidden": false,
    "excludeDirs": [".git"]
  },
  "search": { "debounceMs": 120, "maxResults": 50 },
  "defaults": { "fallbackToolId": "vscode" }
}
""");

        var store = new PortableSettingsStore_c(_tempRoot);
        var loaded = store.LoadOrCreate();

        Assert.NotNull(loaded.LastUsedProjectTools);
        Assert.Empty(loaded.LastUsedProjectTools);
        Assert.NotNull(loaded.Onboarding);
        Assert.Equal(OnboardingStates_c.Pending, loaded.Onboarding.State);
        Assert.NotEmpty(loaded.Defaults.ToolPriorityOrder);
    }

    [Fact]
    public void LoadOrCreate_MigratesOnboardingVersionMismatchToPending()
    {
        Directory.CreateDirectory(_tempRoot);
        var settingsPath = Path.Combine(_tempRoot, "launcher.settings.json");
        File.WriteAllText(settingsPath, """
{
  "version": 1,
  "hotkeys": { "toggle": "Alt+Shift+Space" },
  "roots": [],
  "toolPaths": [],
  "projectOverrides": [],
  "lastUsedProjectTools": [],
  "indexing": {
    "maxDepth": 20,
    "followSymlinks": false,
    "includeHidden": false,
    "excludeDirs": [".git"]
  },
  "search": { "debounceMs": 120, "maxResults": 50 },
  "defaults": { "fallbackToolId": "vscode", "toolPriorityOrder": [] },
  "onboarding": { "version": 0, "state": "completed" }
}
""");

        var store = new PortableSettingsStore_c(_tempRoot);
        var loaded = store.LoadOrCreate();

        Assert.Equal(OnboardingSettings_c.CurrentVersion_c, loaded.Onboarding.Version);
        Assert.Equal(OnboardingStates_c.Pending, loaded.Onboarding.State);
    }

    [Fact]
    public void LoadOrCreate_HotkeyConflicts_ResetGeneralHotkeysToDefaults()
    {
        Directory.CreateDirectory(_tempRoot);
        var settingsPath = Path.Combine(_tempRoot, "launcher.settings.json");
        File.WriteAllText(settingsPath, """
{
  "version": 1,
  "hotkeys": { "toggle": "Alt+Shift+Space" },
  "generalHotkeys": {
    "exit": "Enter",
    "moveUp": "Up",
    "moveDown": "Down",
    "confirm": "Enter",
    "switchMode": "Shift+Tab",
    "alternativeLaunchModifiers": "Shift+Alt",
    "alternativeLaunchKeys": ["Z", "X"]
  },
  "roots": [],
  "toolPaths": [],
  "projectOverrides": [],
  "lastUsedProjectTools": [],
  "indexing": { "maxDepth": 20, "followSymlinks": false, "includeHidden": false, "excludeDirs": [".git"] },
  "search": { "debounceMs": 120, "maxResults": 50 },
  "defaults": { "fallbackToolId": "vscode", "toolPriorityOrder": [] },
  "onboarding": { "version": 1, "state": "completed" }
}
""");

        var store = new PortableSettingsStore_c(_tempRoot);
        var loaded = store.LoadOrCreate();

        Assert.Equal("Escape", loaded.GeneralHotkeys.Exit);
        Assert.Equal("Enter", loaded.GeneralHotkeys.Confirm);
        Assert.Equal(["Z", "X", "C", "V", "B", "N", "M"], loaded.GeneralHotkeys.AlternativeLaunchKeys);
    }

    [Fact]
    public void LoadOrCreate_InvalidThemeOverride_FallsBackToDefault()
    {
        Directory.CreateDirectory(_tempRoot);
        var settingsPath = Path.Combine(_tempRoot, "launcher.settings.json");
        File.WriteAllText(settingsPath, """
{
  "version": 1,
  "themeOverride": "Neon",
  "hotkeys": { "toggle": "Alt+Shift+Space" },
  "roots": [],
  "toolPaths": [],
  "projectOverrides": [],
  "lastUsedProjectTools": [],
  "indexing": { "maxDepth": 20, "followSymlinks": false, "includeHidden": false, "excludeDirs": [".git"] },
  "search": { "debounceMs": 120, "maxResults": 50 },
  "defaults": { "fallbackToolId": "vscode", "toolPriorityOrder": [] },
  "onboarding": { "version": 1, "state": "completed" }
}
""");

        var store = new PortableSettingsStore_c(_tempRoot);
        var loaded = store.LoadOrCreate();

        Assert.Equal(ThemeOverrideOptions_c.Default, loaded.ThemeOverride);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Ignore.
        }
    }
}
