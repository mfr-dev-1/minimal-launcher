# ADR-0012: Global Tool Priority Precedence

- Status: Accepted
- Date: 2026-02-28

## Context
Search currently combines project overrides, last-used tool, and landmark heuristics.
Onboarding introduces global preferred tool ordering.
Behavior must stay deterministic and preserve project-level authority.

## Decision
Resolved candidate precedence:
1. Project-level override (if present)
2. Last-used tool for the same project
3. Global `Defaults.ToolPriorityOrder` (only when no project override is active)
4. Landmark-based resolver output
5. `Defaults.FallbackToolId`
6. First available detected tool (final runtime fallback)

Additional rule:
- Global priority is not injected when a project override exists.

## Consequences
- User-wide preference affects default suggestion for non-overridden projects.
- Explicit project overrides remain authoritative.
- Alternative tool rows still include additional detected tools after preferred candidates.
