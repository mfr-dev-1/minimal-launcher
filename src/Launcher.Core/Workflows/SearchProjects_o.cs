namespace Launcher.Core.Workflows;

public sealed class SearchProjects_o
{
    private readonly SearchProjects_c _searchCompute = new();

    public IReadOnlyList<Launcher.Core.Models.SearchResult_c> Execute(
        string rawQuery,
        IReadOnlyList<Launcher.Core.Models.ProjectRecord_c> projects,
        IReadOnlyList<Launcher.Core.Models.ToolRecord_c> tools,
        Launcher.Core.Models.LauncherSettings_c settings)
    {
        var query = (rawQuery ?? string.Empty).Trim();
        var availableTools = _searchCompute.BuildAvailableToolList(tools);
        var availableToolsById = _searchCompute.BuildAvailableToolMap(availableTools);
        var filteredProjects = _searchCompute.FilterProjects(query, projects);
        var unsortedResults = _searchCompute.BuildSearchResults(query, filteredProjects, availableTools, availableToolsById, settings);
        return _searchCompute.OrderAndTrimResults(unsortedResults, settings.Search.MaxResults);
    }
}
