# ADR-0005: Search Ranking and Debounce

- Status: Accepted
- Date: 2026-02-28

## Context
Need responsive search with predictable ordering for keyboard-driven launch flow.

## Decision
- Debounce input at 220ms.
- Cancel stale searches on each keystroke.
- Score by: prefix, boundary, contiguous match, path length, recency.
- Sort by score desc, lastOpened desc, name asc.

## Consequences
- Responsive and deterministic result ordering.
- Ranking is local and dependency-free.
- Future tuning can adjust weights without changing external schema.
