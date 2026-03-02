namespace Launcher.Infrastructure.ApplicationPorts;

public sealed class ToolDetectionPort_c : Launcher.Application.Ports.IToolDetectionPort_c
{
    private readonly Launcher.Infrastructure.Tools.ToolDetectionWorkflow_o _innerWorkflow;

    public ToolDetectionPort_c(Launcher.Infrastructure.Tools.ToolDetectionWorkflow_o innerWorkflow)
    {
        _innerWorkflow = innerWorkflow;
    }

    public List<Launcher.Core.Models.ToolRecord_c> Detect(Launcher.Core.Models.LauncherSettings_c settings)
    {
        return _innerWorkflow.Detect(settings);
    }
}
