namespace Launcher.Infrastructure.ApplicationPorts;

public sealed class ProjectLaunchPort_c : Launcher.Application.Ports.IProjectLaunchPort_c
{
    private readonly Launcher.Infrastructure.Launch.ProjectLauncher_c _innerLauncher;

    public ProjectLaunchPort_c(Launcher.Infrastructure.Launch.ProjectLauncher_c innerLauncher)
    {
        _innerLauncher = innerLauncher;
    }

    public bool Launch(Launcher.Core.Models.ProjectRecord_c project, Launcher.Core.Models.ToolRecord_c tool)
    {
        return _innerLauncher.Launch(project, tool);
    }

    public bool LaunchFile(Launcher.Core.Models.ProjectRecord_c project, Launcher.Core.Models.ToolRecord_c tool, string filePath)
    {
        return _innerLauncher.LaunchFile(project, tool, filePath);
    }

    public bool OpenPathInShell(string path)
    {
        return _innerLauncher.OpenPathInShell(path);
    }

    public bool LaunchWindowsTerminal(string projectPath)
    {
        return _innerLauncher.LaunchWindowsTerminal(projectPath);
    }
}
