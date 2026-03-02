using Launcher.Core.Workflows;

namespace Launcher.Core.Tests;

public sealed class PowerModeCommandParser_c_Tests
{
    private readonly PowerModeCommandParser_c _sut = new();

    [Fact]
    public void Parse_WindowsTerminal_ReturnsWtermCommand()
    {
        var parsed = _sut.Parse(">myproj wterm");

        Assert.True(parsed.IsCommand);
        Assert.True(parsed.IsValid);
        Assert.True(parsed.IsWindowsTerminal);
        Assert.Equal("myproj", parsed.ProjectHint);
    }

    [Fact]
    public void Parse_Unknown_ReturnsInvalid()
    {
        var parsed = _sut.Parse(">launcher dance");

        Assert.True(parsed.IsCommand);
        Assert.False(parsed.IsValid);
        Assert.Equal("Not implemented command.", parsed.Error);
    }

    [Fact]
    public void Parse_LegacyLauncherCommands_ReturnInvalid()
    {
        var edit = _sut.Parse(">launcher edit");
        var kill = _sut.Parse(">launcher kill");

        Assert.False(edit.IsValid);
        Assert.Equal("Not implemented command.", edit.Error);
        Assert.False(kill.IsValid);
        Assert.Equal("Not implemented command.", kill.Error);
    }

    [Fact]
    public void Parse_MetaExit_ReturnsMetaExitCommand()
    {
        var parsed = _sut.Parse(">> exit");

        Assert.True(parsed.IsCommand);
        Assert.True(parsed.IsValid);
        Assert.True(parsed.IsMetaExit);
    }

    [Fact]
    public void Parse_MetaSettings_ReturnsMetaConfigCommand()
    {
        var parsed = _sut.Parse(">> settings");

        Assert.True(parsed.IsCommand);
        Assert.True(parsed.IsValid);
        Assert.True(parsed.IsMetaConfig);
    }

    [Fact]
    public void Parse_MetaConfig_ReturnsInvalid()
    {
        var parsed = _sut.Parse(">> config");

        Assert.True(parsed.IsCommand);
        Assert.False(parsed.IsValid);
        Assert.Equal("Not implemented meta command.", parsed.Error);
    }

    [Fact]
    public void Parse_MetaRefresh_ReturnsMetaRefreshCommand()
    {
        var parsed = _sut.Parse(">> refresh");

        Assert.True(parsed.IsCommand);
        Assert.True(parsed.IsValid);
        Assert.True(parsed.IsMetaRefresh);
    }

    [Fact]
    public void Parse_MetaIndex_ReturnsMetaIndexCommand()
    {
        var parsed = _sut.Parse(">> index");

        Assert.True(parsed.IsCommand);
        Assert.True(parsed.IsValid);
        Assert.True(parsed.IsMetaIndex);
    }

    [Fact]
    public void Parse_MetaOnboarding_ReturnsMetaOnboardingCommand()
    {
        var parsed = _sut.Parse(">> onboarding");

        Assert.True(parsed.IsCommand);
        Assert.True(parsed.IsValid);
        Assert.True(parsed.IsMetaOnboarding);
    }
}
