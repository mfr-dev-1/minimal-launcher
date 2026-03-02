# ADR-0004: Tool Detection Matrix

- Status: Accepted
- Date: 2026-02-28

## Context
Need robust IDE/editor auto-detect with user override for edge cases.

## Decision
Built-in matrix includes:
Visual Studio, VS Code, JetBrains family (IntelliJ/PyCharm/WebStorm/Rider/CLion/GoLand/PhpStorm/RustRover), Android Studio, Eclipse, Cursor, Windsurf, Sublime Text, Notepad++, Vim, Neovim.

Detection precedence:
1. user-defined path
2. PATH lookup
3. known install dirs
4. registry app paths fallback

Special handling:
- Visual Studio via `vswhere`.
- Neovim includes Chocolatey shim path.

## Consequences
- Strong default coverage.
- Long-tail tools still supported via user-defined path override.
- Catalog maintenance required as vendor install paths evolve.
