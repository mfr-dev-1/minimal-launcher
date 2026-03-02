namespace Launcher.Core.Models;

public static class Landmarks_c
{
    public static readonly string[] All =
    [
        ".sln",
        ".idea",
        ".vscode",
        ".code-workspace",
        "package.json",
        "pyproject.toml",
        "requirements.txt",
        "pom.xml",
        "build.gradle",
        "settings.gradle",
        "Cargo.toml",
        "go.mod",
        "composer.json",
        "AGENTS.md",
        "CLAUDE.md",
        "GEMINI.md",
        "COPILOT.md",
        ".cursorrules",
        ".windsurfrules",
        ".clinerules",
        ".aider.conf.yml"
    ];

    public static readonly HashSet<string> FolderLandmarks = new(StringComparer.OrdinalIgnoreCase)
    {
        ".idea",
        ".vscode"
    };
}

public static class ToolIds_c
{
    public const string VisualStudio = "visual_studio";
    public const string VsCode = "vscode";
    public const string IntelliJ = "jetbrains_intellij";
    public const string PyCharm = "jetbrains_pycharm";
    public const string WebStorm = "jetbrains_webstorm";
    public const string Rider = "jetbrains_rider";
    public const string CLion = "jetbrains_clion";
    public const string GoLand = "jetbrains_goland";
    public const string PhpStorm = "jetbrains_phpstorm";
    public const string RustRover = "jetbrains_rustrover";
    public const string AndroidStudio = "android_studio";
    public const string Eclipse = "eclipse";
    public const string Cursor = "cursor";
    public const string Windsurf = "windsurf";
    public const string SublimeText = "sublime_text";
    public const string NotepadPlusPlus = "notepadpp";
    public const string Vim = "vim";
    public const string NeoVim = "neovim";
    public const string WindowsTerminal = "windows_terminal";
}
