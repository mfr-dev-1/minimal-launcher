# ADR-0010: Bulk Suffix Migration Strategy (`_o` and `_c`)

- Status: Accepted
- Date: 2026-02-28

## Context
Codebase still contains widespread `_smp` and `_vrb` suffixes in types, files, and methods.
`AGENTS.md` now standardizes code files toward:
1. Orchestrator (`*_o.*`)
2. Caretaker (`*_c.*`)

Incremental per-file rename is slow and leaves long-lived mixed naming.

## Decision
Use a bulk mechanical migration for remaining code:
1. Replace `_smp` and `_vrb` identifiers to `_c`.
2. Promote known workflow/state coordinators to `_o`:
- `LauncherRuntime_o`
- `FileProjectIndexer_o`
- `ToolDetectionWorkflow_o`
3. Rename corresponding source files to match type names.
4. Keep behavior unchanged; migration is naming/structure normalization only.

## Consequences
- Significant churn in references, but lower long-tail inconsistency.
- Improves rule compliance quickly across app/core/infrastructure/tests.
- Requires full compile + test pass after migration.
