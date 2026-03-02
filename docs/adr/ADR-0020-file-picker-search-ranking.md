# ADR-0020: File Picker Query Search and Text-First Ranking

- Status: Accepted
- Date: 2026-03-01
- Supersedes: none

## Context
File picker mode currently uses alphanumeric key jump by first filename character.
That behavior conflicts with direct query typing and does not support search-style narrowing.
It also treats binary and text files equally, which is suboptimal for developer editing flows.

## Decision
1. Replace file-picker alphanumeric jump behavior with query filtering via the main search input.
2. Keep `Esc` exit and `Enter` open behaviors unchanged.
3. In file-picker mode, rank known text extensions above unknown files and known binaries.
4. Use lightweight in-memory scoring over indexed file entries:
- filename/path exact-prefix-contains-subsequence matching
- text-vs-binary type weighting

## Consequences
1. Typing in file-picker mode now narrows results instead of changing selection by first letter.
2. Developer-relevant text files surface earlier when query matches collide with binaries.
3. Existing file enumeration limits and async loading behavior remain unchanged.
