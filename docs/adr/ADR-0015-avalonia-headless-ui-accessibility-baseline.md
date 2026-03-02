# ADR-0015: Avalonia Headless UI Accessibility Baseline

- Status: Accepted
- Date: 2026-03-01
- Supersedes: none

## Context
`Launcher.App.Headless.Tests` mainly covered viewmodel behavior.
UI regression risk remained for control hierarchy and accessibility metadata because no tests instantiated `MainWindow`.

## Decision
1. Keep Avalonia headless test stack (`Avalonia.Headless.XUnit`) and add window-level tests.
2. Assert stable UI anchors by control name (`SearchBox`, `ResultList`, onboarding controls) instead of brittle style internals.
3. Require `AutomationProperties.Name` on primary interactive controls and validate it in tests.

## Consequences
- Faster detection of accidental XAML regressions that break keyboard-first and assistive workflows.
- Accessibility intent becomes explicit and test-enforced.
- Visual quality is still validated manually; headless tests do not replace real rendering QA.
