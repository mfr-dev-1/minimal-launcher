# ADR-0021: Settings Theme Selection Override

- Status: Accepted
- Date: 2026-03-01
- Supersedes: none

## Context
Launcher currently boots with `RequestedThemeVariant="Default"` and has no user-facing setting to force light or dark.
Users need explicit control over theme variant from settings with immediate feedback.

## Decision
1. Add persisted setting `LauncherSettings_c.ThemeOverride` with allowed values: `Default`, `Light`, `Dark`.
2. Expose setting in UI as settings `Theme` ComboBox.
3. Normalize invalid values to `Default` in `PortableSettingsStore_c`.
4. Apply override in runtime:
- on viewmodel initialization (startup path),
- on settings save (live apply).

## Consequences
1. Theme selection is explicit, persisted, and stable across restarts.
2. Invalid/manual JSON values do not break startup; they fall back safely.
3. Existing default behavior remains unchanged when override is `Default`.
