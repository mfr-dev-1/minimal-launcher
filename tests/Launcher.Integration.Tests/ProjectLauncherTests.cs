using Launcher.Core.Models;
using Launcher.Infrastructure.Launch;

namespace Launcher.Integration.Tests;

public sealed class ProjectLauncher_c_Tests
{
    [Fact]
    public void ResolveArguments_VsCode_UsesReuseFlag()
    {
        var project = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\alpha",
            ProjectName = "alpha"
        };

        var args = ProjectLauncher_c.ResolveArguments(ToolIds_c.VsCode, project, targetFilePath: null);

        Assert.Equal(["-r", @"D:\src\alpha"], args);
    }

    [Fact]
    public void ResolveArguments_UnsupportedTool_FallsBackToProjectPath()
    {
        var project = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\alpha",
            ProjectName = "alpha"
        };

        var args = ProjectLauncher_c.ResolveArguments(ToolIds_c.Rider, project, targetFilePath: null);

        Assert.Equal([@"D:\src\alpha"], args);
    }
}
