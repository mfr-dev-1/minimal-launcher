# ADR-0003: Indexing and Watchers

- Status: Accepted
- Date: 2026-02-28

## Context
Need fast search with large project trees and avoid UI freeze.

## Decision
- Async startup full scan of configured roots.
- `FileSystemWatcher` per root with 300ms event coalescing.
- Reconcile full refresh every 15 minutes.
- Constraints: max depth 20, skip hidden, skip symlink/reparse, skip excluded dirs.

## Consequences
- Fast query after initial index.
- Handles most live changes while tolerating watcher overflow via reconcile.
- Additional background IO; bounded by exclusions and max depth.
