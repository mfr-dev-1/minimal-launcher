using System.Text.Json;
using Launcher.Core.Models;

namespace Launcher.Infrastructure.Storage;

public sealed class PortableSettingsStore_c
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;

    public PortableSettingsStore_c(string baseDirectory)
    {
        _settingsPath = Path.Combine(baseDirectory, "launcher.settings.json");
    }

    public string SettingsPath => _settingsPath;

    public LauncherSettings_c LoadOrCreate()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = LauncherSettings_c.CreateDefault();
            Save(defaults);
            return defaults;
        }

        var content = File.ReadAllText(_settingsPath);
        var parsed = JsonSerializer.Deserialize<LauncherSettings_c>(content, JsonOptions);
        if (parsed is null)
        {
            var defaults = LauncherSettings_c.CreateDefault();
            Save(defaults);
            return defaults;
        }

        parsed.Indexing ??= IndexingSettings_c.CreateDefault();
        parsed.Search ??= new SearchSettings_c();
        parsed.Defaults ??= DefaultSettings_c.CreateDefault();
        parsed.Hotkeys ??= new HotkeySettings_c();
        parsed.GeneralHotkeys ??= GeneralHotkeySettings_c.CreateDefault();
        parsed.Terminal ??= TerminalSettings_c.CreateDefault();
        parsed.AiChat ??= AiChatSettings_c.CreateDefault();
        parsed.Roots ??= [];
        parsed.ToolPaths ??= [];
        parsed.ProjectOverrides ??= [];
        parsed.LastUsedProjectTools ??= [];
        parsed.Onboarding ??= OnboardingSettings_c.CreatePending();
        parsed.Behavior ??= BehaviorSettings_c.CreateDefault();
        parsed.ThemeOverride = NormalizeThemeOverride_c(parsed.ThemeOverride);

        if (string.IsNullOrWhiteSpace(parsed.Defaults.FallbackToolId))
        {
            parsed.Defaults.FallbackToolId = ToolIds_c.VsCode;
        }

        parsed.Defaults.ToolPriorityOrder = NormalizeToolPriorityOrder_c(
            parsed.Defaults.ToolPriorityOrder,
            parsed.Defaults.FallbackToolId);
        parsed.GeneralHotkeys = NormalizeGeneralHotkeys_c(parsed.GeneralHotkeys, parsed.Hotkeys.Toggle);
        parsed.Terminal = NormalizeTerminalSettings_c(parsed.Terminal);
        parsed.AiChat = NormalizeAiChatSettings_c(parsed.AiChat);

        if (parsed.Onboarding.Version != OnboardingSettings_c.CurrentVersion_c)
        {
            parsed.Onboarding.Version = OnboardingSettings_c.CurrentVersion_c;
            parsed.Onboarding.State = OnboardingStates_c.Pending;
        }

        parsed.Onboarding.State = NormalizeOnboardingState_c(parsed.Onboarding.State);

        return parsed;
    }

    public void Save(LauncherSettings_c settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var content = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, content);
    }

    private static List<string> NormalizeToolPriorityOrder_c(IReadOnlyList<string>? raw, string fallbackToolId)
    {
        var merged = new List<string>();

        if (raw is not null)
        {
            merged.AddRange(raw.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        merged.Add(fallbackToolId);
        merged.AddRange(DefaultSettings_c.BuildDefaultToolPriorityOrder_c());
        return merged
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeOnboardingState_c(string? raw)
    {
        if (string.Equals(raw, OnboardingStates_c.Defaulted, StringComparison.OrdinalIgnoreCase))
        {
            return OnboardingStates_c.Defaulted;
        }

        if (string.Equals(raw, OnboardingStates_c.Skipped, StringComparison.OrdinalIgnoreCase))
        {
            return OnboardingStates_c.Skipped;
        }

        if (string.Equals(raw, OnboardingStates_c.Completed, StringComparison.OrdinalIgnoreCase))
        {
            return OnboardingStates_c.Completed;
        }

        return OnboardingStates_c.Pending;
    }

    private static string NormalizeThemeOverride_c(string? raw)
    {
        if (string.Equals(raw, ThemeOverrideOptions_c.Light, StringComparison.OrdinalIgnoreCase))
        {
            return ThemeOverrideOptions_c.Light;
        }

        if (string.Equals(raw, ThemeOverrideOptions_c.Dark, StringComparison.OrdinalIgnoreCase))
        {
            return ThemeOverrideOptions_c.Dark;
        }

        return ThemeOverrideOptions_c.Default;
    }

    private static GeneralHotkeySettings_c NormalizeGeneralHotkeys_c(GeneralHotkeySettings_c raw, string toggleHotkey)
    {
        var defaults = GeneralHotkeySettings_c.CreateDefault();
        var normalized = new GeneralHotkeySettings_c
        {
            Exit = NormalizeHotkey_c(raw.Exit) ?? defaults.Exit,
            MoveUp = NormalizeHotkey_c(raw.MoveUp) ?? defaults.MoveUp,
            MoveDown = NormalizeHotkey_c(raw.MoveDown) ?? defaults.MoveDown,
            Confirm = NormalizeHotkey_c(raw.Confirm) ?? defaults.Confirm,
            SwitchMode = NormalizeHotkey_c(raw.SwitchMode) ?? defaults.SwitchMode,
            AlternativeLaunchModifiers = NormalizeModifierOnlyHotkey_c(raw.AlternativeLaunchModifiers) ?? defaults.AlternativeLaunchModifiers,
            AlternativeLaunchKeys = (raw.AlternativeLaunchKeys ?? [])
                .Select(NormalizeKeyOnlyHotkey_c)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        if (normalized.AlternativeLaunchKeys.Count == 0)
        {
            normalized.AlternativeLaunchKeys = GeneralHotkeySettings_c.BuildDefaultAlternativeLaunchKeys_c();
        }

        if (HasGeneralHotkeyConflicts_c(normalized, toggleHotkey))
        {
            return defaults;
        }

        return normalized;
    }

    private static TerminalSettings_c NormalizeTerminalSettings_c(TerminalSettings_c raw)
    {
        var defaults = TerminalSettings_c.CreateDefault();
        var normalizedExecutable = string.IsNullOrWhiteSpace(raw.ShellExecutable)
            ? defaults.ShellExecutable
            : raw.ShellExecutable.Trim();
        var normalizedPrefix = raw.ShellArgumentsPrefix is { Count: > 0 }
            ? raw.ShellArgumentsPrefix
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
            : defaults.ShellArgumentsPrefix;

        return new TerminalSettings_c
        {
            ShellExecutable = normalizedExecutable,
            ShellArgumentsPrefix = normalizedPrefix.Count == 0
                ? TerminalSettings_c.BuildDefaultShellArgumentPrefix_c()
                : normalizedPrefix
        };
    }

    private static AiChatSettings_c NormalizeAiChatSettings_c(AiChatSettings_c raw)
    {
        return new AiChatSettings_c
        {
            CliExecutable = raw.CliExecutable?.Trim() ?? string.Empty,
            ArgumentTemplate = string.IsNullOrWhiteSpace(raw.ArgumentTemplate) ? "--prompt {prompt}" : raw.ArgumentTemplate.Trim(),
            ContextDirectory = raw.ContextDirectory?.Trim() ?? string.Empty,
            TimeoutSeconds = Math.Clamp(raw.TimeoutSeconds <= 0 ? 45 : raw.TimeoutSeconds, 5, 300)
        };
    }

    private static bool HasGeneralHotkeyConflicts_c(GeneralHotkeySettings_c settings, string toggleHotkey)
    {
        var allChords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddNormalizedChord_c(allChords, NormalizeHotkey_c(toggleHotkey));
        if (!AddNormalizedChord_c(allChords, settings.Exit))
        {
            return true;
        }

        if (!AddNormalizedChord_c(allChords, settings.MoveUp))
        {
            return true;
        }

        if (!AddNormalizedChord_c(allChords, settings.MoveDown))
        {
            return true;
        }

        if (!AddNormalizedChord_c(allChords, settings.Confirm))
        {
            return true;
        }

        if (!AddNormalizedChord_c(allChords, settings.SwitchMode))
        {
            return true;
        }

        foreach (var key in settings.AlternativeLaunchKeys)
        {
            var combined = $"{settings.AlternativeLaunchModifiers}+{key}";
            var normalized = NormalizeHotkey_c(combined);
            if (!AddNormalizedChord_c(allChords, normalized))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AddNormalizedChord_c(HashSet<string> sink, string? chord)
    {
        if (string.IsNullOrWhiteSpace(chord))
        {
            return true;
        }

        return sink.Add(chord);
    }

    private static string? NormalizeHotkey_c(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var parts = raw
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        if (parts.Count == 0)
        {
            return null;
        }

        var modifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? key = null;
        foreach (var part in parts)
        {
            var normalized = NormalizeModifierToken_c(part);
            if (normalized is not null)
            {
                modifiers.Add(normalized);
                continue;
            }

            if (key is not null)
            {
                return null;
            }

            key = NormalizeKeyOnlyHotkey_c(part);
        }

        if (key is null)
        {
            return null;
        }

        return BuildCanonicalChord_c(modifiers, key);
    }

    private static string? NormalizeModifierOnlyHotkey_c(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var parts = raw
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        if (parts.Count == 0)
        {
            return null;
        }

        var modifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            var normalized = NormalizeModifierToken_c(part);
            if (normalized is null)
            {
                return null;
            }

            modifiers.Add(normalized);
        }

        return BuildCanonicalChord_c(modifiers, key: null);
    }

    private static string NormalizeKeyOnlyHotkey_c(string? raw)
    {
        return raw?.Trim() ?? string.Empty;
    }

    private static string? NormalizeModifierToken_c(string raw)
    {
        if (string.Equals(raw, "Ctrl", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "Control", StringComparison.OrdinalIgnoreCase))
        {
            return "Ctrl";
        }

        if (string.Equals(raw, "Alt", StringComparison.OrdinalIgnoreCase))
        {
            return "Alt";
        }

        if (string.Equals(raw, "Shift", StringComparison.OrdinalIgnoreCase))
        {
            return "Shift";
        }

        if (string.Equals(raw, "Win", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            return "Win";
        }

        return null;
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
}
