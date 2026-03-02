# ADR-0014: PowerToys-Run Visual Direction For Launcher UI

- Status: Accepted
- Date: 2026-03-01
- Supersedes: none

## Context
Launcher moved to Avalonia and needs a more production-grade visual language.
The current surface has inconsistent hierarchy and reads as custom-themed rather than an established Windows developer tool.
Requested target is a professional PowerToys Run imitation.

## Decision
1. Use a neutral dark, Fluent-aligned palette with restrained accent usage.
2. Keep dense single-row search results and explicit keyboard affordance hints.
3. Preserve existing behavior/bindings; apply cosmetic-only changes in XAML and theme variant preference.
4. Keep dependency footprint unchanged (Avalonia core only).

## Consequences
- UI appears more familiar to Windows power users.
- Visual consistency improves across search, list, footer hints, and onboarding overlay.
- Pixel-perfect parity with PowerToys is not guaranteed; direction prioritizes usability and maintainability.
- Future theme flexibility (light mode) is reduced until explicit theming support is added.
