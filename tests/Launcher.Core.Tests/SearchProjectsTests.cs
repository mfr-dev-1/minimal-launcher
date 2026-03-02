using Launcher.Core.Models;
using Launcher.Core.Workflows;

namespace Launcher.Core.Tests;

public sealed class SearchProjects_c_Tests
{
    [Fact]
    public void Execute_OrdersByScoreThenRecencyThenName()
    {
        var settings = LauncherSettings_c.CreateDefault();
        settings.Search.MaxResults = 10;

        var tools = new List<ToolRecord_c>
        {
            new() { ToolId = ToolIds_c.VsCode, DisplayName = "VS Code", ExecutablePath = "code.exe", IsAvailable = true }
        };

        var first = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\alpha",
            ProjectName = "alpha",
            Landmarks = [".vscode"],
            LastOpenedUtc = DateTime.UtcNow.AddHours(-1)
        };

        var second = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\alpine",
            ProjectName = "alpine",
            Landmarks = [".vscode"],
            LastOpenedUtc = DateTime.UtcNow.AddHours(-24)
        };

        var sut = new SearchProjects_o();

        var results = sut.Execute("alp", [first, second], tools, settings);

        Assert.Equal(2, results.Count);
        Assert.Equal("alpha", results[0].Project.ProjectName);
        Assert.Equal("alpine", results[1].Project.ProjectName);
    }

    [Fact]
    public void Execute_AppliesProjectOverrideBeforeLandmarkRules()
    {
        var settings = LauncherSettings_c.CreateDefault();
        settings.ProjectOverrides.Add(new ProjectToolOverride_c
        {
            ProjectPath = @"D:\src\alpha",
            ToolId = ToolIds_c.Rider
        });

        var tools = new List<ToolRecord_c>
        {
            new() { ToolId = ToolIds_c.Rider, DisplayName = "Rider", ExecutablePath = "rider.exe", IsAvailable = true },
            new() { ToolId = ToolIds_c.VisualStudio, DisplayName = "Visual Studio", ExecutablePath = "devenv.exe", IsAvailable = true },
            new() { ToolId = ToolIds_c.VsCode, DisplayName = "VS Code", ExecutablePath = "code.exe", IsAvailable = true }
        };

        var project = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\alpha",
            ProjectName = "alpha",
            Landmarks = [".sln"]
        };

        var sut = new SearchProjects_o();
        var results = sut.Execute("alpha", [project], tools, settings);

        Assert.Single(results);
        Assert.Equal(ToolIds_c.Rider, results[0].SuggestedTool.ToolId);
    }

    [Fact]
    public void Execute_IncludesNonRuleInstalledToolsInAlternatives()
    {
        var settings = LauncherSettings_c.CreateDefault();

        var tools = new List<ToolRecord_c>
        {
            new() { ToolId = ToolIds_c.VisualStudio, DisplayName = "Visual Studio", ExecutablePath = "devenv.exe", IsAvailable = true },
            new() { ToolId = ToolIds_c.VsCode, DisplayName = "VS Code", ExecutablePath = "code.exe", IsAvailable = true },
            new() { ToolId = ToolIds_c.NotepadPlusPlus, DisplayName = "Notepad++", ExecutablePath = "notepad++.exe", IsAvailable = true }
        };

        var project = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\alpha",
            ProjectName = "alpha",
            Landmarks = [".sln"]
        };

        var sut = new SearchProjects_o();
        var results = sut.Execute("alpha", [project], tools, settings);

        Assert.Single(results);
        Assert.Equal(ToolIds_c.VisualStudio, results[0].SuggestedTool.ToolId);
        Assert.Contains(results[0].AlternativeTools, x => x.ToolId == ToolIds_c.VsCode);
        Assert.Contains(results[0].AlternativeTools, x => x.ToolId == ToolIds_c.NotepadPlusPlus);
    }

    [Fact]
    public void Execute_UsesLastUsedToolAsDefaultForSameProject()
    {
        var settings = LauncherSettings_c.CreateDefault();
        settings.LastUsedProjectTools.Add(new ProjectLastUsedTool_c
        {
            ProjectPath = @"D:\src\alpha",
            ToolId = ToolIds_c.VsCode,
            LastUsedUtc = DateTime.UtcNow
        });

        var tools = new List<ToolRecord_c>
        {
            new() { ToolId = ToolIds_c.VisualStudio, DisplayName = "Visual Studio", ExecutablePath = "devenv.exe", IsAvailable = true },
            new() { ToolId = ToolIds_c.VsCode, DisplayName = "VS Code", ExecutablePath = "code.exe", IsAvailable = true }
        };

        var project = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\alpha",
            ProjectName = "alpha",
            Landmarks = [".sln"]
        };

        var sut = new SearchProjects_o();
        var results = sut.Execute("alpha", [project], tools, settings);

        Assert.Single(results);
        Assert.Equal(ToolIds_c.VsCode, results[0].SuggestedTool.ToolId);
        Assert.Contains(results[0].AlternativeTools, x => x.ToolId == ToolIds_c.VisualStudio);
    }

    [Fact]
    public void Execute_UsesGlobalToolPriorityWhenNoProjectOverride()
    {
        var settings = LauncherSettings_c.CreateDefault();
        settings.Defaults.ToolPriorityOrder = [ToolIds_c.Rider, ToolIds_c.VisualStudio, ToolIds_c.VsCode];

        var tools = new List<ToolRecord_c>
        {
            new() { ToolId = ToolIds_c.Rider, DisplayName = "Rider", ExecutablePath = "rider.exe", IsAvailable = true },
            new() { ToolId = ToolIds_c.VisualStudio, DisplayName = "Visual Studio", ExecutablePath = "devenv.exe", IsAvailable = true },
            new() { ToolId = ToolIds_c.VsCode, DisplayName = "VS Code", ExecutablePath = "code.exe", IsAvailable = true }
        };

        var project = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\alpha",
            ProjectName = "alpha",
            Landmarks = [".sln"]
        };

        var sut = new SearchProjects_o();
        var results = sut.Execute("alpha", [project], tools, settings);

        Assert.Single(results);
        Assert.Equal(ToolIds_c.Rider, results[0].SuggestedTool.ToolId);
    }

    [Fact]
    public void Execute_ProjectOverrideStillWinsOverGlobalToolPriority()
    {
        var settings = LauncherSettings_c.CreateDefault();
        settings.Defaults.ToolPriorityOrder = [ToolIds_c.VsCode, ToolIds_c.VisualStudio, ToolIds_c.Rider];
        settings.ProjectOverrides.Add(new ProjectToolOverride_c
        {
            ProjectPath = @"D:\src\alpha",
            ToolId = ToolIds_c.Rider
        });

        var tools = new List<ToolRecord_c>
        {
            new() { ToolId = ToolIds_c.Rider, DisplayName = "Rider", ExecutablePath = "rider.exe", IsAvailable = true },
            new() { ToolId = ToolIds_c.VisualStudio, DisplayName = "Visual Studio", ExecutablePath = "devenv.exe", IsAvailable = true },
            new() { ToolId = ToolIds_c.VsCode, DisplayName = "VS Code", ExecutablePath = "code.exe", IsAvailable = true }
        };

        var project = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\alpha",
            ProjectName = "alpha",
            Landmarks = [".sln"]
        };

        var sut = new SearchProjects_o();
        var results = sut.Execute("alpha", [project], tools, settings);

        Assert.Single(results);
        Assert.Equal(ToolIds_c.Rider, results[0].SuggestedTool.ToolId);
    }

    [Fact]
    public void Execute_FuzzySequenceQuery_MatchesWithoutDirectSubstring()
    {
        var settings = LauncherSettings_c.CreateDefault();
        var tools = new List<ToolRecord_c>
        {
            new() { ToolId = ToolIds_c.VsCode, DisplayName = "VS Code", ExecutablePath = "code.exe", IsAvailable = true }
        };

        var project = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\launcher",
            ProjectName = "launcher",
            Landmarks = [".vscode"]
        };

        var sut = new SearchProjects_o();
        var results = sut.Execute("lnchr", [project], tools, settings);

        Assert.Single(results);
        Assert.Equal("launcher", results[0].Project.ProjectName);
    }
}
