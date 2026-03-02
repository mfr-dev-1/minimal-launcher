using System.Globalization;

namespace Launcher.Core.Workflows;

public sealed class FuzzyProjectScorer_c
{
    public bool IsMatch(string query, Launcher.Core.Models.ProjectRecord_c project)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var normalizedQuery = query.Trim();
        return CalculateBaseScore(normalizedQuery, project.ProjectName) > 0 ||
               CalculateBaseScore(normalizedQuery, project.ProjectPath) > 0;
    }

    public int Score(string query, Launcher.Core.Models.ProjectRecord_c project)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return project.LastOpenedUtc.HasValue ? 1000 : 0;
        }

        var normalizedQuery = query.Trim();
        var normalizedName = project.ProjectName;
        var normalizedPath = project.ProjectPath;

        var nameScore = CalculateBaseScore(normalizedQuery, normalizedName);
        var pathScore = CalculateBaseScore(normalizedQuery, normalizedPath) / 2;
        var recencyBonus = project.LastOpenedUtc.HasValue
            ? (int)Math.Clamp((DateTime.UtcNow - project.LastOpenedUtc.Value).TotalHours switch
            {
                <= 24 => 150,
                <= 168 => 80,
                _ => 20
            }, 0, 150)
            : 0;

        var shorterPathBonus = Math.Max(0, 80 - (project.ProjectPath.Length / 6));

        return nameScore + pathScore + recencyBonus + shorterPathBonus;
    }

    private static int CalculateBaseScore(string query, string source)
    {
        var sourceSpan = source.AsSpan();
        var querySpan = query.AsSpan();

        var prefixBonus = source.StartsWith(query, true, CultureInfo.InvariantCulture) ? 300 : 0;
        var boundaryBonus = ContainsWordBoundaryMatch(sourceSpan, querySpan) ? 180 : 0;
        var contiguousBonus = source.Contains(query, StringComparison.OrdinalIgnoreCase) ? 160 : 0;
        var sequenceScore = CalculateSequenceScore(sourceSpan, querySpan);

        return prefixBonus + boundaryBonus + contiguousBonus + sequenceScore;
    }

    private static bool ContainsWordBoundaryMatch(ReadOnlySpan<char> source, ReadOnlySpan<char> query)
    {
        if (query.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < source.Length; i++)
        {
            var isBoundary = i == 0 || !char.IsLetterOrDigit(source[i - 1]);
            if (!isBoundary)
            {
                continue;
            }

            if (i + query.Length > source.Length)
            {
                continue;
            }

            if (source[i..(i + query.Length)].Equals(query, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int CalculateSequenceScore(ReadOnlySpan<char> source, ReadOnlySpan<char> query)
    {
        if (query.Length == 0)
        {
            return 0;
        }

        var sourceIndex = 0;
        var queryIndex = 0;
        var contiguousRun = 0;
        var bestRun = 0;

        while (sourceIndex < source.Length && queryIndex < query.Length)
        {
            if (char.ToUpperInvariant(source[sourceIndex]) == char.ToUpperInvariant(query[queryIndex]))
            {
                contiguousRun++;
                bestRun = Math.Max(bestRun, contiguousRun);
                queryIndex++;
            }
            else
            {
                contiguousRun = 0;
            }

            sourceIndex++;
        }

        if (queryIndex < query.Length)
        {
            return 0;
        }

        return (query.Length * 20) + (bestRun * 25);
    }
}
