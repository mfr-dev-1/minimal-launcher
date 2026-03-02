# ADR-0017: APP Gap Closure ADR Gates

- Status: Accepted
- Date: 2026-03-01
- Supersedes: none

## Context
`20260301-app-gap-closure-scope-and-adr-gates.md` identified unresolved decisions that block implementation slices `S1-S9`.

## Decision
1. Open-hotkey semantics:
- Keep hotkey behavior as `open-only`.
- If launcher window is already visible, pressing the open hotkey does nothing.

2. Result row canonical format:
- Canonical row is `single compact row` (`project | path | icon`).
- Two-row rendering is not used for this execution pass.

3. Terminal mode architecture:
- Terminal mode executes each submitted command through configured shell executable + argument prefix.
- Terminal output is persisted in-memory for app lifetime and restored when mode returns to `>`.
- `>>` remains deterministic meta-command mode and is never routed to terminal execution.

4. AI CLI trust/safety:
- AI prompt execution uses direct process invocation (`UseShellExecute=false`), not shell-eval.
- Prompt/context values are injected as process arguments via placeholder substitution (`{prompt}`, `{context}`).
- Prompt text cannot trigger local script execution through command interpolation.

## Consequences
- Behavior now matches APP baseline scenario where open hotkey does not toggle-hide.
- Row rendering ambiguity is removed for current scope.
- Terminal mode remains responsive and stateful without long-lived shell host complexity.
- AI integration keeps a minimal attack surface while allowing configurable CLI arguments.
