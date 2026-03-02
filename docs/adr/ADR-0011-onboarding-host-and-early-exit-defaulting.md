# ADR-0011: Onboarding Host Mode and Early-Exit Defaulting

- Status: Accepted
- Date: 2026-02-28

## Context
Launcher now needs a first-run onboarding flow without hard-blocking usage.
Window lifecycle already auto-hides on deactivation and escape.
We need predictable behavior when onboarding is interrupted.

## Decision
1. Host onboarding inside `MainWindow` as an explicit overlay mode.
2. Trigger onboarding when `settings.onboarding.state == pending`.
3. If onboarding exits early while pending (close/hide/deactivate/escape/app-exit), auto-apply sensible defaults and persist `state=defaulted`.
4. Keep explicit `Skip` path with persisted `state=skipped`.
5. Keep explicit completion path with persisted `state=completed`.
6. Allow manual re-entry via `>> onboarding`.

## Consequences
- No modal/secondary window complexity.
- Existing hide-on-deactivate behavior remains consistent.
- First-run users are never blocked, but state transitions are explicit and auditable.
- Onboarding can be reopened later without mutating completed/skipped state unless user finishes again.
