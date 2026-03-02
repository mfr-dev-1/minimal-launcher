namespace Launcher.Core.Workflows;

public sealed class SearchProjects_c
{
    private const int MaxAlternativeTools = 7;

    private readonly FuzzyProjectScorer_c _scorer = new();
    private readonly ToolSuggestionResolver_c _toolSuggestionResolver = new();

    public List<Launcher.Core.Models.ToolRecord_c> BuildAvailableToolList(IReadOnlyList<Launcher.Core.Models.ToolRecord_c> tools)
    {
        return tools
            .Where(x => x.IsAvailable)
            .GroupBy(x => x.ToolId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    public Dictionary<string, Launcher.Core.Models.ToolRecord_c> BuildAvailableToolMap(IReadOnlyList<Launcher.Core.Models.ToolRecord_c> availableTools)
    {
        return availableTools.ToDictionary(x => x.ToolId, x => x, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<Launcher.Core.Models.ProjectRecord_c> FilterProjects(string query, IReadOnlyList<Launcher.Core.Models.ProjectRecord_c> projects)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return projects;
        }

        var normalizedQuery = query.Trim();
        return projects
            .Where(project =>
                project.ProjectName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                project.ProjectPath.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                _scorer.IsMatch(normalizedQuery, project))
            .ToList();
    }

    public List<Launcher.Core.Models.SearchResult_c> BuildSearchResults(
        string query,
        IReadOnlyList<Launcher.Core.Models.ProjectRecord_c> filteredProjects,
        IReadOnlyList<Launcher.Core.Models.ToolRecord_c> availableTools,
        IReadOnlyDictionary<string, Launcher.Core.Models.ToolRecord_c> availableToolsById,
        Launcher.Core.Models.LauncherSettings_c settings)
    {
        var results = new List<Launcher.Core.Models.SearchResult_c>();

        foreach (var project in filteredProjects)
        {
            var candidateToolIds = ResolveCandidateToolIdsForProject(project, settings);
            var selectedTool = candidateToolIds
                .Select(id => availableToolsById.GetValueOrDefault(id))
                .FirstOrDefault(tool => tool is not null);

            selectedTool ??= availableTools.FirstOrDefault();
            if (selectedTool is null)
            {
                continue;
            }

            var preferredAlternativeIds = candidateToolIds
                .Where(id => !string.Equals(id, selectedTool.ToolId, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var preferredAlternatives = preferredAlternativeIds
                .Select(id => availableToolsById.GetValueOrDefault(id))
                .Where(tool => tool is not null)
                .Cast<Launcher.Core.Models.ToolRecord_c>()
                .ToList();

            var remainingAlternatives = availableTools
                .Where(tool => !string.Equals(tool.ToolId, selectedTool.ToolId, StringComparison.OrdinalIgnoreCase))
                .Where(tool => !preferredAlternativeIds.Contains(tool.ToolId, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var alternatives = preferredAlternatives
                .Concat(remainingAlternatives)
                .Take(MaxAlternativeTools)
                .ToList();

            results.Add(new Launcher.Core.Models.SearchResult_c
            {
                Project = project,
                SuggestedTool = selectedTool,
                AlternativeTools = alternatives,
                Score = _scorer.Score(query, project)
            });
        }

        return results;
    }

    public IReadOnlyList<Launcher.Core.Models.SearchResult_c> OrderAndTrimResults(
        IReadOnlyList<Launcher.Core.Models.SearchResult_c> results,
        int maxResults)
    {
        return results
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Project.LastOpenedUtc)
            .ThenBy(x => x.Project.ProjectName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxResults))
            .ToList();
    }

    private IReadOnlyList<string> ResolveCandidateToolIdsForProject(
        Launcher.Core.Models.ProjectRecord_c project,
        Launcher.Core.Models.LauncherSettings_c settings)
    {
        var candidateToolIds = new List<string>();
        var overrideMatch = settings.ProjectOverrides
            .FirstOrDefault(x => string.Equals(x.ProjectPath, project.ProjectPath, StringComparison.OrdinalIgnoreCase));
        var lastUsedMatch = settings.LastUsedProjectTools
            .Where(x => string.Equals(x.ProjectPath, project.ProjectPath, StringComparison.OrdinalIgnoreCase))
            .Where(x => !string.IsNullOrWhiteSpace(x.ToolId))
            .OrderByDescending(x => x.LastUsedUtc)
            .FirstOrDefault();

        if (overrideMatch is not null && !string.IsNullOrWhiteSpace(overrideMatch.ToolId))
        {
            AddToolCandidate_c(candidateToolIds, overrideMatch.ToolId);
            AddToolCandidate_c(candidateToolIds, lastUsedMatch?.ToolId);
        }
        else
        {
            AddToolCandidate_c(candidateToolIds, lastUsedMatch?.ToolId);
            foreach (var toolId in settings.Defaults.ToolPriorityOrder)
            {
                AddToolCandidate_c(candidateToolIds, toolId);
            }
        }

        candidateToolIds.AddRange(_toolSuggestionResolver.ResolveCandidateToolIds(project, settings.Defaults.FallbackToolId));
        AddToolCandidate_c(candidateToolIds, settings.Defaults.FallbackToolId);
        AddToolCandidate_c(candidateToolIds, Launcher.Core.Models.ToolIds_c.VsCode);
        return candidateToolIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void AddToolCandidate_c(List<string> sink, string? toolId)
    {
        if (!string.IsNullOrWhiteSpace(toolId))
        {
            sink.Add(toolId);
        }
    }
}
