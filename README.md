# Launcher

A developer-oriented keyboard-driven launcher for Windows. Open projects in your IDE, run terminal commands, and query an AI assistant — all from a single overlay.

## Features

- **Project search** — fuzzy-search indexed projects, auto-detect best-fit IDE per project
- **IDE launch** — reuses existing windows; supports multiple installed IDEs/editors simultaneously
- **File picker** — inline file browser within a selected project
- **Terminal passthrough** — prefix `>` to route commands to PowerShell (configurable)
- **AI chat** — prefix `?` for a stateless AI prompt session with markdown rendering
- **Meta commands** — prefix `>>` for built-in commands (`exit`, `config`, `refresh`, `index`)

## Requirements

- Windows 10+
- .NET 8

## Getting Started

```
git clone <repo>
cd launcher
dotnet run --project src/Launcher.App
```

Default hotkey to open: `Shift+Alt+Space`

## Build Release

```
dotnet publish src/Launcher.App/Launcher.App.csproj -c Release -r win-x64
```

Output: `src/Launcher.App/bin/Release/net8.0-windows/win-x64/publish/`

Requires .NET 8 on the target machine. Add `--self-contained true` to bundle the runtime.

## Code Architecture

Source files follow a two-type convention:

| Type | Suffix | Role |
|---|---|---|
| **Operator** | `*_o.*` | Workflow & state. Reads like a story — collects context, calls named functions in logical order. No complex logic. |
| **Workhorse** | `*_c.*` | Pure implementation. Dense, detail-oriented computation functions. Minimal hidden state. |

The Operator acts as an **implementation index**: someone reading it should understand the full workflow without diving into implementation details.

## Stack

- [Avalonia UI](https://avaloniaui.net/) — cross-platform UI framework
- [LiveMarkdown.Avalonia](https://github.com/DearVa/LiveMarkdown.Avalonia) — live markdown rendering
