using Launcher.Core.Models;

namespace Launcher.Infrastructure.Tools;

public sealed class InstalledToolCatalog_c
{
    public IReadOnlyList<ToolSpec_c> BuildSpecs()
    {
        return
        [
            new ToolSpec_c(ToolIds_c.VisualStudio, "Visual Studio", ["devenv.exe"], [@"C:\Program Files\Microsoft Visual Studio"], true),
            new ToolSpec_c(ToolIds_c.VsCode, "Visual Studio Code", ["code.exe", "Code.exe"], [@"%LocalAppData%\Programs\Microsoft VS Code"], false),
            new ToolSpec_c(ToolIds_c.IntelliJ, "IntelliJ IDEA", ["idea64.exe"], [@"%ProgramFiles%\JetBrains"], false),
            new ToolSpec_c(ToolIds_c.PyCharm, "PyCharm", ["pycharm64.exe"], [@"%ProgramFiles%\JetBrains"], false),
            new ToolSpec_c(ToolIds_c.WebStorm, "WebStorm", ["webstorm64.exe"], [@"%ProgramFiles%\JetBrains"], false),
            new ToolSpec_c(ToolIds_c.Rider, "Rider", ["rider64.exe"], [@"%ProgramFiles%\JetBrains"], false),
            new ToolSpec_c(ToolIds_c.CLion, "CLion", ["clion64.exe"], [@"%ProgramFiles%\JetBrains"], false),
            new ToolSpec_c(ToolIds_c.GoLand, "GoLand", ["goland64.exe"], [@"%ProgramFiles%\JetBrains"], false),
            new ToolSpec_c(ToolIds_c.PhpStorm, "PhpStorm", ["phpstorm64.exe"], [@"%ProgramFiles%\JetBrains"], false),
            new ToolSpec_c(ToolIds_c.RustRover, "RustRover", ["rustrover64.exe"], [@"%ProgramFiles%\JetBrains"], false),
            new ToolSpec_c(ToolIds_c.AndroidStudio, "Android Studio", ["studio64.exe"], [@"%ProgramFiles%\Android\Android Studio\bin"], false),
            new ToolSpec_c(ToolIds_c.Eclipse, "Eclipse", ["eclipse.exe"], [@"%ProgramFiles%\Eclipse Foundation"], false),
            new ToolSpec_c(ToolIds_c.Cursor, "Cursor", ["Cursor.exe"], [@"%LocalAppData%\Programs\cursor"], false),
            new ToolSpec_c(ToolIds_c.Windsurf, "Windsurf", ["Windsurf.exe"], [@"%LocalAppData%\Programs\Windsurf"], false),
            new ToolSpec_c(ToolIds_c.SublimeText, "Sublime Text", ["sublime_text.exe"], [@"%ProgramFiles%\Sublime Text"], false),
            new ToolSpec_c(ToolIds_c.NotepadPlusPlus, "Notepad++", ["notepad++.exe"], [@"%ProgramFiles%\Notepad++"], false),
            new ToolSpec_c(ToolIds_c.Vim, "Vim", ["vim.exe"], [@"%ProgramFiles%\Vim"], false),
            new ToolSpec_c(ToolIds_c.NeoVim, "Neovim", ["nvim.exe"], [@"C:\ProgramData\chocolatey\bin", @"%ProgramFiles%\Neovim\bin"], false)
        ];
    }
}

public sealed record ToolSpec_c(
    string ToolId,
    string DisplayName,
    IReadOnlyList<string> ExecutableCandidates,
    IReadOnlyList<string> KnownDirectories,
    bool UseVsWhere);
