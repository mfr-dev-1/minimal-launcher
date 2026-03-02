namespace Launcher.Core.Models;

public sealed class ProjectRecord_c
{
    public string ProjectPath { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public List<string> Landmarks { get; set; } = [];

    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastOpenedUtc { get; set; }
}

public sealed class ToolRecord_c
{
    public string ToolId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ExecutablePath { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }
}

public sealed class ToolDetectionResult_c
{
    public List<ToolRecord_c> Tools { get; set; } = [];
}

public sealed class SearchResult_c
{
    public required ProjectRecord_c Project { get; init; }

    public required ToolRecord_c SuggestedTool { get; init; }

    public required List<ToolRecord_c> AlternativeTools { get; init; }

    public int Score { get; init; }
}

public sealed class LaunchRequest_c
{
    public required string ProjectPath { get; init; }

    public required string ToolId { get; init; }
}

public sealed class ProjectIndexSnapshot_c
{
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    public List<ProjectRecord_c> Projects { get; set; } = [];
}
