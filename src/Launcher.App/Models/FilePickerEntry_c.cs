namespace Launcher.App.Models;

public sealed class FilePickerEntry_c
{
    public string FullPath { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;
}
