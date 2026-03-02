# ADR-0016: Startup Readiness Visibility Gate

- Status: Accepted
- Date: 2026-03-01

## Context
Launcher window was shown before runtime initialization completed.
On slow first-run indexing, user could see and interact with an unready UI.
Requirement: app should only be visible and interactable when ready.

## Decision
1. Keep `MainWindow` hidden during framework startup.
2. Gate tray/hotkey toggle actions until runtime init completes.
3. Show onboarding automatically only after runtime is ready.
4. Register global hotkey only when a native window handle exists (defer silently until first open).

## Consequences
- Users no longer interact with partial startup state.
- Startup intent is explicit: tray can report "still starting" instead of exposing incomplete UI.
- Hotkey registration may occur on first window open when app starts hidden.
