using Avalonia.Input;

namespace Launcher.App.Services;

public sealed class InAppHotkeyRouter_c
{
    private readonly HotkeyChord_c? _exit;
    private readonly HotkeyChord_c? _moveUp;
    private readonly HotkeyChord_c? _moveDown;
    private readonly HotkeyChord_c? _confirm;
    private readonly HotkeyChord_c? _switchMode;
    private readonly KeyModifiers _alternativeModifiers;
    private readonly Key[] _alternativeKeys;

    public InAppHotkeyRouter_c(Launcher.Core.Models.GeneralHotkeySettings_c settings)
    {
        _exit = ParseChord_c(settings.Exit);
        _moveUp = ParseChord_c(settings.MoveUp);
        _moveDown = ParseChord_c(settings.MoveDown);
        _confirm = ParseChord_c(settings.Confirm);
        _switchMode = ParseChord_c(settings.SwitchMode);
        _alternativeModifiers = ParseModifierChord_c(settings.AlternativeLaunchModifiers) ?? (KeyModifiers.Shift | KeyModifiers.Alt);
        _alternativeKeys = ParseAlternativeKeys_c(settings.AlternativeLaunchKeys);
    }

    public bool IsExit(Key key, KeyModifiers modifiers)
    {
        return MatchesChord_c(_exit, key, modifiers);
    }

    public bool IsMoveUp(Key key, KeyModifiers modifiers)
    {
        return MatchesChord_c(_moveUp, key, modifiers);
    }

    public bool IsMoveDown(Key key, KeyModifiers modifiers)
    {
        return MatchesChord_c(_moveDown, key, modifiers);
    }

    public bool IsConfirm(Key key, KeyModifiers modifiers)
    {
        return MatchesChord_c(_confirm, key, modifiers);
    }

    public bool IsSwitchMode(Key key, KeyModifiers modifiers)
    {
        return MatchesChord_c(_switchMode, key, modifiers);
    }

    public int ResolveAlternativeSlot(Key key, KeyModifiers modifiers)
    {
        if (!MatchesModifierChord_c(_alternativeModifiers, modifiers))
        {
            return -1;
        }

        return Array.IndexOf(_alternativeKeys, key);
    }

    private static Key[] ParseAlternativeKeys_c(IReadOnlyList<string>? raw)
    {
        var parsed = (raw ?? [])
            .Select(ParseKeyOnly_c)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToArray();
        return parsed.Length == 0 ? [Key.Z, Key.X, Key.C, Key.V, Key.B, Key.N, Key.M] : parsed;
    }

    private static HotkeyChord_c? ParseChord_c(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var parts = raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var modifiers = KeyModifiers.None;
        Key? key = null;

        foreach (var part in parts)
        {
            if (TryParseModifier_c(part, out var modifier))
            {
                modifiers |= modifier;
                continue;
            }

            var parsedKey = ParseKeyOnly_c(part);
            if (!parsedKey.HasValue || key.HasValue)
            {
                return null;
            }

            key = parsedKey.Value;
        }

        if (!key.HasValue)
        {
            return null;
        }

        var canonicalChord = CanonicalizeChord_c(key.Value, modifiers);
        return new HotkeyChord_c { Key = canonicalChord.Key, Modifiers = canonicalChord.Modifiers };
    }

    private static KeyModifiers? ParseModifierChord_c(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var parts = raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var modifiers = KeyModifiers.None;
        foreach (var part in parts)
        {
            if (!TryParseModifier_c(part, out var modifier))
            {
                return null;
            }

            modifiers |= modifier;
        }

        return modifiers;
    }

    private static Key? ParseKeyOnly_c(string raw)
    {
        if (Enum.TryParse<Key>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        if (raw.Length == 1 && char.IsLetter(raw[0]))
        {
            if (Enum.TryParse<Key>(raw.ToUpperInvariant(), ignoreCase: true, out parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool TryParseModifier_c(string raw, out KeyModifiers modifier)
    {
        if (string.Equals(raw, "Shift", StringComparison.OrdinalIgnoreCase))
        {
            modifier = KeyModifiers.Shift;
            return true;
        }

        if (string.Equals(raw, "Alt", StringComparison.OrdinalIgnoreCase))
        {
            modifier = KeyModifiers.Alt;
            return true;
        }

        if (string.Equals(raw, "Ctrl", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "Control", StringComparison.OrdinalIgnoreCase))
        {
            modifier = KeyModifiers.Control;
            return true;
        }

        if (string.Equals(raw, "Win", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            modifier = KeyModifiers.Meta;
            return true;
        }

        modifier = KeyModifiers.None;
        return false;
    }

    private static bool MatchesChord_c(HotkeyChord_c? chord, Key key, KeyModifiers modifiers)
    {
        if (chord is null)
        {
            return false;
        }

        var canonicalIncoming = CanonicalizeChord_c(key, modifiers);
        return canonicalIncoming.Key == chord.Key && MatchesModifierChord_c(chord.Modifiers, canonicalIncoming.Modifiers);
    }

    private static bool MatchesModifierChord_c(KeyModifiers expected, KeyModifiers actual)
    {
        var actualMasked = actual & (KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Meta);
        return actualMasked == expected;
    }

    private sealed class HotkeyChord_c
    {
        public Key Key { get; init; }

        public KeyModifiers Modifiers { get; init; }
    }

    private static HotkeyChord_c CanonicalizeChord_c(Key key, KeyModifiers modifiers)
    {
        var maskedModifiers = modifiers & (KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Meta);

        // Avalonia may report Shift+Tab as OemBackTab without the Shift modifier.
        if (key == Key.OemBackTab)
        {
            key = Key.Tab;
            maskedModifiers |= KeyModifiers.Shift;
        }

        return new HotkeyChord_c
        {
            Key = key,
            Modifiers = maskedModifiers
        };
    }
}
