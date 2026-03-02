namespace Launcher.App.Models;

public sealed class IdeOptionItem_c
{
    public string SlotLabel { get; set; } = string.Empty;

    public string ToolName { get; set; } = string.Empty;

    public string LaunchToken { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public Avalonia.Media.IImage? ToolIcon { get; set; }
}
