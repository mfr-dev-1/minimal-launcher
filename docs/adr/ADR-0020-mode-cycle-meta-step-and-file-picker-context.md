# ADR-0020: Shift+Tab Mode Cycle Uses Meta Step and Excludes File Picker

- Status: Accepted
- Date: 2026-03-01
- Supersedes: none

## Context
`Shift+Tab` is the in-window mode switch gesture.
Mode cycle previously included file picker when a file picker session existed.
Product direction now requires:
1. Cycle order must be `Project -> AI -> Terminal -> Meta -> Project`.
2. File picker is contextual launch flow, not an independent mode in this cycle.

## Decision
1. Keep `Shift+Tab` as the switch-mode gesture.
2. Treat meta as an explicit cycle step by switching from terminal into project-search meta input (`>>`).
3. From meta step, next `Shift+Tab` returns to normal project search (clears meta input).
4. From file picker, `Shift+Tab` jumps to AI (same cycle progression as project-origin flow), not to file picker as a cycle step.

## Consequences
1. Cycle is deterministic and user-visible: `Project -> AI -> Terminal -> Meta -> Project`.
2. File picker remains available only as project launch context and `Esc` exit path.
3. Meta mode remains implemented via project-search + `>>` projection, not a new enum mode.
