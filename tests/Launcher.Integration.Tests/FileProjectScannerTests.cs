using Launcher.Core.Models;
using Launcher.Infrastructure.Indexing;

namespace Launcher.Integration.Tests;

public sealed class FileProjectScanner_c_Tests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "launcher-int-" + Guid.NewGuid().ToString("N"));

    public FileProjectScanner_c_Tests()
    {
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void ScanProjects_SkipsExcludedAndHiddenAndRespectsMaxDepth()
    {
        var allowed = Path.Combine(_tempRoot, "allowed-project");
        var excludedParent = Path.Combine(_tempRoot, "node_modules", "nested-project");
        var hidden = Path.Combine(_tempRoot, "hidden-project");
        var tooDeep = Path.Combine(_tempRoot, "d1", "d2", "d3", "d4", "deep-project");

        Directory.CreateDirectory(allowed);
        Directory.CreateDirectory(excludedParent);
        Directory.CreateDirectory(hidden);
        Directory.CreateDirectory(tooDeep);

        File.WriteAllText(Path.Combine(allowed, "app.sln"), string.Empty);
        File.WriteAllText(Path.Combine(excludedParent, "app.sln"), string.Empty);
        File.WriteAllText(Path.Combine(hidden, "app.sln"), string.Empty);
        File.WriteAllText(Path.Combine(tooDeep, "app.sln"), string.Empty);

        var hiddenInfo = new DirectoryInfo(hidden);
        hiddenInfo.Attributes |= FileAttributes.Hidden;

        var scanner = new FileProjectScanner_c();
        var settings = IndexingSettings_c.CreateDefault();
        settings.MaxDepth = 3;

        var results = scanner.ScanProjects(
            [new RootPathSetting_c { Path = _tempRoot, Enabled = true }],
            settings,
            CancellationToken.None);

        Assert.Contains(results, x => string.Equals(x.ProjectPath, allowed, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(results, x => string.Equals(x.ProjectPath, excludedParent, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(results, x => string.Equals(x.ProjectPath, hidden, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(results, x => string.Equals(x.ProjectPath, tooDeep, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ScanProjects_SkipsSymbolicLinks_WhenFollowSymlinksFalse()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var realDir = Path.Combine(_tempRoot, "real");
        var linkDir = Path.Combine(_tempRoot, "link");
        Directory.CreateDirectory(realDir);
        File.WriteAllText(Path.Combine(realDir, "app.sln"), string.Empty);

        try
        {
            Directory.CreateSymbolicLink(linkDir, realDir);
        }
        catch
        {
            return;
        }

        var scanner = new FileProjectScanner_c();
        var settings = IndexingSettings_c.CreateDefault();
        settings.FollowSymlinks = false;

        var results = scanner.ScanProjects(
            [new RootPathSetting_c { Path = _tempRoot, Enabled = true }],
            settings,
            CancellationToken.None);

        Assert.DoesNotContain(results, x => string.Equals(x.ProjectPath, linkDir, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ScanProjects_RecognizesAgenticHarnessLandmarks()
    {
        var harnessProject = Path.Combine(_tempRoot, "agentic-project");
        Directory.CreateDirectory(harnessProject);
        File.WriteAllText(Path.Combine(harnessProject, "AGENTS.md"), "# agent");

        var scanner = new FileProjectScanner_c();
        var settings = IndexingSettings_c.CreateDefault();

        var results = scanner.ScanProjects(
            [new RootPathSetting_c { Path = _tempRoot, Enabled = true }],
            settings,
            CancellationToken.None);

        var match = results.FirstOrDefault(x => string.Equals(x.ProjectPath, harnessProject, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(match);
        Assert.Contains("AGENTS.md", match!.Landmarks, StringComparer.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        try
        {
            var dir = new DirectoryInfo(_tempRoot);
            foreach (var nested in dir.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                nested.Attributes = FileAttributes.Normal;
            }

            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // Ignore cleanup failures in test temp area.
        }
    }
}
