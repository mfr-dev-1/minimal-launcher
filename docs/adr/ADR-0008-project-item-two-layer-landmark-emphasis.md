# ADR-0008: Project Item Two-Layer Layout with Landmark Emphasis

- Status: Accepted
- Date: 2026-02-28

## Context
Current project row is single-line compact text. Landmark visibility is weak, and user cannot quickly see why a default IDE was selected.

## Decision
Project item presentation will use two text layers:
1. Top layer (medium text): project identifier.
2. Bottom layer (small text): `landmark files if available | full path`.

Landmark emphasis rule:
- Highlight landmark token that wins IDE selection (or first-ranked matching landmark when multiple candidates exist).

If no landmark exists:
- Bottom layer shows full path only.

## Consequences
- Better scanability and debuggability of IDE suggestion outcome.
- Slightly taller row height and denser visual layout constraints.
- Requires result-row view model to carry winning-landmark metadata.
