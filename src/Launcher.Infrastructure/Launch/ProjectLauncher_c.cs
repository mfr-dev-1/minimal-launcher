using System.Diagnostics;
using Launcher.Core.Models;

namespace Launcher.Infrastructure.Launch;

public sealed class ProjectLauncher_c
{
    private static readonly HashSet<string> JetBrainsToolIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ToolIds_c.IntelliJ,
        ToolIds_c.PyCharm,
        ToolIds_c.WebStorm,
        ToolIds_c.Rider,
        ToolIds_c.CLion,
        ToolIds_c.GoLand,
        ToolIds_c.PhpStorm,
        ToolIds_c.RustRover,
        ToolIds_c.AndroidStudio,
    };

    private static readonly HashSet<string> WindowActivationToolIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ToolIds_c.VisualStudio,
        ToolIds_c.VsCode,
        ToolIds_c.Cursor,
        ToolIds_c.Windsurf,
        ToolIds_c.Eclipse,
        ToolIds_c.SublimeText,
    };

    public bool Launch(ProjectRecord_c project, ToolRecord_c tool)
    {
        return LaunchCore_c(project, tool, targetFilePath: null);
    }

    public bool LaunchFile(ProjectRecord_c project, ToolRecord_c tool, string targetFilePath)
    {
        if (string.IsNullOrWhiteSpace(targetFilePath) || !File.Exists(targetFilePath))
        {
            return false;
        }

        return LaunchCore_c(project, tool, targetFilePath);
    }

    private static bool LaunchCore_c(ProjectRecord_c project, ToolRecord_c tool, string? targetFilePath)
    {
        if (!tool.IsAvailable || string.IsNullOrWhiteSpace(tool.ExecutablePath))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = tool.ExecutablePath,
            UseShellExecute = true,
            WorkingDirectory = project.ProjectPath
        };

        foreach (var arg in ResolveArguments(tool.ToolId, project, targetFilePath))
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (targetFilePath == null && WindowActivationToolIds.Contains(tool.ToolId))
        {
            var processName = Path.GetFileNameWithoutExtension(tool.ExecutablePath);
            var folderName = Path.GetFileName(project.ProjectPath.TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (WindowActivation_c.TryActivateProjectWindow(processName, folderName))
                return true;
        }

        Process.Start(startInfo);
        return true;
    }

    public bool OpenPathInShell(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool LaunchWindowsTerminal(string projectPath)
    {
        if (!Directory.Exists(projectPath))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "wt.exe",
            UseShellExecute = true,
            WorkingDirectory = projectPath
        };

        Process.Start(startInfo);
        return true;
    }

    public static IReadOnlyList<string> ResolveArguments(string toolId, ProjectRecord_c project, string? targetFilePath)
    {
        var launchTarget = string.IsNullOrWhiteSpace(targetFilePath)
            ? project.ProjectPath
            : targetFilePath;

        if (string.Equals(toolId, ToolIds_c.VsCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(toolId, ToolIds_c.Cursor, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(toolId, ToolIds_c.Windsurf, StringComparison.OrdinalIgnoreCase))
        {
            return [launchTarget];
        }

        if (JetBrainsToolIds.Contains(toolId))
            return ["--reuse-instance", launchTarget];

        if (toolId == ToolIds_c.VisualStudio)
        {
            var sln = Directory.EnumerateFiles(project.ProjectPath, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(sln))
            {
                return [sln];
            }
        }

        return [launchTarget];
    }
}
