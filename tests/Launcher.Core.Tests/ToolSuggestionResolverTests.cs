using Launcher.Core.Models;
using Launcher.Core.Workflows;

namespace Launcher.Core.Tests;

public sealed class ToolSuggestionResolver_c_Tests
{
    private readonly ToolSuggestionResolver_c _sut = new();

    [Fact]
    public void ResolveCandidateToolIds_WhenSlnExists_PrioritizesVisualStudioThenRider()
    {
        var project = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\api",
            ProjectName = "api",
            Landmarks = [".sln"]
        };

        var result = _sut.ResolveCandidateToolIds(project, ToolIds_c.VsCode);

        Assert.Equal(ToolIds_c.VisualStudio, result[0]);
        Assert.Equal(ToolIds_c.Rider, result[1]);
    }

    [Fact]
    public void ResolveCandidateToolIds_WhenCargoTomlExists_PrioritizesRustRover()
    {
        var project = new ProjectRecord_c
        {
            ProjectPath = @"D:\src\rust-app",
            ProjectName = "rust-app",
            Landmarks = ["Cargo.toml"]
        };

        var result = _sut.ResolveCandidateToolIds(project, ToolIds_c.VsCode);

        Assert.Equal(ToolIds_c.RustRover, result[0]);
        Assert.Equal(ToolIds_c.VsCode, result[1]);
    }
}
