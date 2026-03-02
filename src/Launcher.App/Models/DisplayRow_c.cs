namespace Launcher.App.Models;

public enum DisplayRowKind_c
{
    Project = 0,
    Meta = 1,
    Terminal = 2,
    FilePicker = 3,
    Empty = 4,
    TerminalError = 5
}

public sealed class DisplayRow_c
{
    public string ProjectIdentifier { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public Avalonia.Media.IImage? DefaultIdeIcon { get; set; }

    public string DefaultIdeName { get; set; } = string.Empty;

    public bool IsIconVisible { get; set; } = true;

    public bool IsIconSeparatorVisible { get; set; } = true;

    public DisplayRowKind_c RowKind { get; set; } = DisplayRowKind_c.Project;

    public bool IsTerminalLikeRow =>
        RowKind == DisplayRowKind_c.Terminal ||
        RowKind == DisplayRowKind_c.TerminalError ||
        RowKind == DisplayRowKind_c.Empty;

    public bool IsTerminalErrorRow => RowKind == DisplayRowKind_c.TerminalError;

    public bool IsTerminalStandardRow => IsTerminalLikeRow && !IsTerminalErrorRow;

    public bool IsStructuredRow =>
        RowKind == DisplayRowKind_c.Project || RowKind == DisplayRowKind_c.Meta || RowKind == DisplayRowKind_c.FilePicker;
}
