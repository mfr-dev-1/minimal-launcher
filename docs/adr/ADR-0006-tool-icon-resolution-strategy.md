# ADR-0006: Tool Icon Resolution Strategy

- Status: Accepted
- Date: 2026-02-28

## Context
Current IDE icon uses same 4-box glyph for all tools, only color changes. Recognition is weak (example: Notepad++).

## Decision
Resolve IDE/editor icon in this order:
1. Extract executable-associated icon from detected tool executable path.
2. If extraction fails, generate a compact glyph badge from tool id/display name.

Apply in search result row and IDE option bar.
Cache by `toolId|executablePath` to avoid repeated extraction work during typing/navigation.

## Consequences
- Installed tools show their native brand icon with no extra dependency.
- Fallback still distinct per tool even when executable icon is unavailable.
- Slight extra startup/render CPU on first icon resolve; amortized via cache.
