# ADR-0013: Avalonia Windows-First Migration

- Status: Accepted
- Date: 2026-02-28
- Supersedes: ADR-0001

## Context
Launcher V1 shipped on WPF for delivery speed.
Current direction requires:
- framework-independent application orchestration boundaries,
- MVVM migration for clearer UI state management,
- retention of Windows-first launcher ergonomics (global hotkey + tray + overlay behavior),
- no extra UI framework dependency beyond Avalonia core.

## Decision
1. Replace WPF UI host with Avalonia in-place in `Launcher.App`.
2. Keep production target Windows-first (`net8.0-windows`), including Win32 hotkey interop.
3. Introduce `Launcher.Application` as service-boundary layer between UI and infrastructure.
4. Use MVVM structure for Avalonia UI without ReactiveUI.
5. Preserve existing launcher behavior parity, allowing only minor UX quality improvements.

## Consequences
- UI stack is no longer tied to `System.Windows` types.
- Runtime orchestration can be tested independently from UI framework.
- Win32-specific integrations remain explicit and controlled in app services.
- Migration complexity increases short term due to dual work (boundary extraction + UI swap).
- Future non-Windows support is easier but intentionally not in current scope.
