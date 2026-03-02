# ADR-0009: Search Flow Orchestrator/Caretaker Migration Slice

- Status: Accepted
- Date: 2026-02-28

## Context
`AGENTS.md` enforces two code file types:
1. Orchestrator (`*_o.*`)
2. Caretaker (`*_c.*`)

Current search flow was in `SearchProjects_smp` with mixed state orchestration + dense computation.

## Decision
Refactor search flow as first migration slice:
1. Introduce `SearchProjects_o` as workflow orchestrator.
2. Move dense search logic into `SearchProjects_c`.
3. Rename search helper computation classes to caretaker suffix:
- `FuzzyProjectScorer_c`
- `ToolSuggestionResolver_c`
4. Keep behavior compatible (ranking, alternative-tool selection, override precedence unchanged).

## Consequences
- Search flow now follows explicit orchestration/computation split.
- Runtime now depends on `SearchProjects_o`.
- Existing naming convention (`_smp`/`_vrb`) still exists in non-search areas and will require further slices.
