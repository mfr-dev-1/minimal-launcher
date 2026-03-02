using Launcher.Core.Models;
using Launcher.Infrastructure.Tools;

namespace Launcher.Integration.Tests;

public sealed class InstalledToolDetector_c_Tests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "launcher-tool-" + Guid.NewGuid().ToString("N"));

    public InstalledToolDetector_c_Tests()
    {
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void DetectSingle_UsesUserOverrideBeforeOtherSources()
    {
        var fakeExe = Path.Combine(_tempRoot, "custom-code.exe");
        File.WriteAllText(fakeExe, string.Empty);

        var settings = LauncherSettings_c.CreateDefault();
        settings.ToolPaths.Add(new ToolPathOverride_c
        {
            ToolId = ToolIds_c.VsCode,
            Path = fakeExe
        });

        var spec = new ToolSpec_c(
            ToolIds_c.VsCode,
            "Visual Studio Code",
            ["code.exe"],
            [],
            false);

        var sut = new InstalledToolDetector_c();
        var result = sut.DetectSingle(spec, settings);

        Assert.True(result.IsAvailable);
        Assert.Equal(fakeExe, result.ExecutablePath);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // Ignore.
        }
    }
}
