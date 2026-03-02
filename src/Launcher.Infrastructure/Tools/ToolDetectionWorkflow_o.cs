using Launcher.Core.Models;

namespace Launcher.Infrastructure.Tools;

public sealed class ToolDetectionWorkflow_o
{
    private readonly InstalledToolCatalog_c _catalog = new();
    private readonly InstalledToolDetector_c _detector = new();

    public List<ToolRecord_c> Detect(LauncherSettings_c settings)
    {
        var specs = _catalog.BuildSpecs();
        var tools = new List<ToolRecord_c>(specs.Count);

        foreach (var spec in specs)
        {
            tools.Add(_detector.DetectSingle(spec, settings));
        }

        return tools;
    }
}
