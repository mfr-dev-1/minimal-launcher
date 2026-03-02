# ADR-0019: Disable Index Update Forced Rerender

- Status: Accepted
- Date: 2026-03-01
- Supersedes: none

## Context
`FileProjectIndexer_o` emits `ProjectsUpdated` for watcher and reconcile-driven refreshes.
`LauncherApplication_o` forwards that as `IndexUpdated`.
`MainWindowViewModel_o` forced projection refresh on `IndexUpdated`, causing unsolicited UI rerender during background index activity.

## Decision
1. Keep indexing/watcher/reconcile behavior unchanged.
2. Remove forced projection rerender on `IndexUpdated` altogether.
3. Do not expose any settings flag or UI toggle for this behavior.

## Consequences
1. Background index work never forces UI rerender.
2. Manual index commands (`>> index`, `>> refresh`) update runtime index data but do not trigger automatic result rerender.
3. UI projection updates remain user-driven (typing/search/mode transitions).
