# ADR-0002: Portable Storage

- Status: Accepted
- Date: 2026-02-28

## Context
V1 should be portable and avoid installer/admin requirements.

## Decision
Store settings/index beside executable:
- `launcher.settings.json`
- `launcher.index.json`

## Consequences
- Portable by default.
- Easy backup/move between machines.
- Must handle write failures for read-only locations gracefully in future slices.
