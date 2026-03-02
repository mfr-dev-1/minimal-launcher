# ADR-0018: UI Polish Balanced Refresh (Fixed-Size, Dark-First)

- Status: Accepted
- Date: 2026-03-01
- Supersedes: none

## Context
Launcher UI behavior is in place, but visual and interaction consistency gaps remain:
hardcoded non-token colors, static mode copy, cramped terminal rows, and settings focus friction.

## Decision
1. Keep fixed-size launcher shell and existing interaction flow.
2. Apply dark-first token normalization with light parity:
- add shared tokens for overlay scrim, chrome action button, and primary/secondary action buttons.
- remove hardcoded control colors outside theme dictionaries.
3. Make header/search/footer mode-aware:
- dynamic hint text, search watermark, search prefix, and alternative-launch modifier label.
4. Split row rendering by row kind without changing list virtualization:
- structured row for project/meta/file-picker.
- full-width monospace row for terminal and empty states.
5. Keep current hide-on-deactivate and escape semantics.
6. Improve keyboard ergonomics by focusing primary settings field when settings open.

## Consequences
- UI gains stronger cross-mode coherence without behavior changes.
- Light mode no longer inherits dark-specific hardcoded control colors.
- Terminal output readability improves in fixed-size layout.
- Headless tests can lock dynamic copy, focus routing, and row-kind rendering behavior.
