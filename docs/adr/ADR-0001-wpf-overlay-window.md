# ADR-0001: WPF Overlay Window

- Status: Superseded by ADR-0013
- Date: 2026-02-28

## Context
Need Windows-first launcher with minimal dependencies, no-admin preference, fast delivery.

## Decision
Use WPF on .NET 8. Main launcher UI is a borderless centered overlay with `Topmost=true` only while visible and `ShowInTaskbar=false`.

## Consequences
- Mature Windows integration for global hotkey/tray workflows.
- Lower implementation risk than WinUI/Avalonia for V1.
- Visual layer is conventional but sufficient for speed and responsiveness goals.
