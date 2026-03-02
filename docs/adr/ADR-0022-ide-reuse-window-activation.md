# ADR-0022: IDE Reuse / Window Activation Strategy

**Date:** 2026-03-02
**Status:** Accepted (updated 2026-03-02)

## Context

When launching a project in an IDE that is already open, spawning a new process wastes resources and disrupts developer flow. Three tiers of IDE reuse are needed:

| Tier | IDEs | Strategy |
|---|---|---|
| 1 | VSCode, Cursor, Windsurf | Window activation (Win32) |
| 2 | All JetBrains IDEs, Android Studio | `--reuse-instance` flag |
| 3 | Visual Studio, Eclipse, Sublime Text | Window activation (Win32) |

## Decision

**Tier 1 (VSCode, Cursor, Windsurf) — updated:** Originally used the `-r` CLI flag (reuse-window). Dropped in favor of direct Win32 window activation. Rationale: CLI startup overhead makes `-r` noticeably slower than alt-tab; Win32 `SetForegroundWindow` is near-instantaneous. Window activation only applies to project-level launches; file-level launches (`LaunchFile`) still use CLI so the target file is actually opened.

**Tier 2:** Pass `--reuse-instance <path>` via `ResolveArguments`. JetBrains Toolbox-installed IDEs all support this flag as of 2023+.

**Tier 1 & 3 (window activation):** Before spawning a new process, call `WindowActivation_c.TryActivateProjectWindow`:
1. `Process.GetProcessesByName(processName)` — processName derived from `tool.ExecutablePath` at runtime, no mapping table needed.
2. `EnumWindows` (user32) to enumerate all top-level windows and match PIDs.
3. Read window title via `GetWindowText`; match `projectFolderName` case-insensitively.
4. If matched: `ShowWindow(SW_RESTORE)` + `SetForegroundWindow` → return true (skip new launch).
5. If no match: fall through to normal `Process.Start`.

**Window title patterns relied upon:**
- VS Code: `"FolderName — Visual Studio Code"`
- Cursor: `"FolderName — Cursor"`
- Windsurf: `"FolderName — Windsurf"`
- Visual Studio: `"SolutionName - Microsoft Visual Studio"` (folder ≈ sln name in common case)
- Sublime Text: `"FolderName - Sublime Text"`
- Eclipse: `"workspace - Eclipse IDE"` (folder ≈ workspace name in common case)

## Alternatives rejected

| Alternative | Reason rejected |
|---|---|
| `-r` / `--reuse-window` CLI flag (VSCode) | CLI process startup adds ~200–500 ms; Win32 activation is instant |
| `Process.MainWindowHandle` | Returns `IntPtr.Zero` for minimized/background windows; unreliable |
| WMI command-line scanning | Requires WMI, slow (~100 ms+), heavy dependency |
| COM DTE automation (VS only) | Requires COM reference, elevated complexity, VS-specific |
| Window class name matching | Requires per-IDE research, brittle across versions |

## Known limitations (accepted)

- **VS:** If solution name ≠ folder name, match fails → new instance launched (same as before).
- **Eclipse:** Workspace may differ from project folder → match may fail.
- **UAC elevation mismatch:** `SetForegroundWindow` may silently fail if IDE runs elevated and launcher does not (UIPI restriction). New instance launched as fallback.
- **Tier 2 (JetBrains):** If JetBrains IDE is not running, `--reuse-instance` simply starts a new instance — correct behavior.
- **VSCode file launches:** Window activation skipped; CLI used so the specific file is opened.
