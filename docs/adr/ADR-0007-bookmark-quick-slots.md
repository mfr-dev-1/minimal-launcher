# ADR-0007: Bookmark Quick Slots and Command Routing

- Status: Accepted
- Date: 2026-02-28

## Context
Need fast project recall without typing. User requested bookmark flow with default add shortcut, startup visibility, and numeric power command.

## Decision
Bookmark behavior:
1. `Ctrl+B` adds/updates bookmark for current selected project into first available slot.
2. Slots are `1..9` (fixed count, deterministic order).
3. Power command `>1`..`>9` opens corresponding bookmark slot.
4. On launcher open, before first search debounce is processed, show bookmark list immediately.
5. If command is not `>1`..`>9`, existing navigation/select behavior remains unchanged.

Planned storage:
- Persist bookmarks in portable settings JSON as stable project-path based slots.

## Consequences
- Faster reopen flow for frequently used projects.
- Preserves current search/list UX for non-bookmark usage.
- Requires command parser, settings schema, and startup-render state updates.
