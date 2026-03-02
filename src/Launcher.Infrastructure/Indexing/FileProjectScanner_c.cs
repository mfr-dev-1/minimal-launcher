using System.Collections.Concurrent;
using Launcher.Core.Models;

namespace Launcher.Infrastructure.Indexing;

public sealed class FileProjectScanner_c
{
    private readonly ConcurrentDictionary<string, bool> _landmarkCache = new(StringComparer.OrdinalIgnoreCase);

    public List<ProjectRecord_c> ScanProjects(IReadOnlyList<RootPathSetting_c> roots, IndexingSettings_c settings, CancellationToken cancellationToken)
    {
        _landmarkCache.Clear();
        var projects = new Dictionary<string, ProjectRecord_c>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots.Where(x => x.Enabled && !string.IsNullOrWhiteSpace(x.Path)))
        {
            if (!Directory.Exists(root.Path))
            {
                continue;
            }

            ScanRoot(root.Path, settings, projects, cancellationToken);
        }

        return projects.Values
            .OrderBy(x => x.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ScanRoot(
        string rootPath,
        IndexingSettings_c settings,
        Dictionary<string, ProjectRecord_c> projects,
        CancellationToken cancellationToken)
    {
        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((rootPath, 0));

        var excludes = new HashSet<string>(settings.ExcludeDirs, StringComparer.OrdinalIgnoreCase);

        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = queue.Dequeue();
            if (current.Depth > settings.MaxDepth)
            {
                continue;
            }

            if (!IsDirectoryEligible(current.Path, settings, excludes))
            {
                continue;
            }

            var landmarks = FindLandmarks(current.Path);
            if (landmarks.Count > 0)
            {
                var name = Path.GetFileName(current.Path);
                projects[current.Path] = new ProjectRecord_c
                {
                    ProjectPath = current.Path,
                    ProjectName = string.IsNullOrWhiteSpace(name) ? current.Path : name,
                    Landmarks = landmarks,
                    LastSeenUtc = DateTime.UtcNow
                };
            }

            if (current.Depth == settings.MaxDepth)
            {
                continue;
            }

            IEnumerable<string> childDirs;
            try
            {
                childDirs = Directory.EnumerateDirectories(current.Path);
            }
            catch
            {
                continue;
            }

            foreach (var child in childDirs)
            {
                queue.Enqueue((child, current.Depth + 1));
            }
        }
    }

    private bool IsDirectoryEligible(string path, IndexingSettings_c settings, HashSet<string> excludes)
    {
        var dirName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(dirName) && excludes.Contains(dirName))
        {
            return false;
        }

        DirectoryInfo directoryInfo;
        try
        {
            directoryInfo = new DirectoryInfo(path);
            var attributes = directoryInfo.Attributes;

            if (!settings.IncludeHidden && attributes.HasFlag(FileAttributes.Hidden))
            {
                return false;
            }

            if (!settings.FollowSymlinks && attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    private List<string> FindLandmarks(string path)
    {
        var landmarks = new List<string>();

        foreach (var landmark in Landmarks_c.All)
        {
            var cacheKey = $"{path}|{landmark}";
            var exists = _landmarkCache.GetOrAdd(cacheKey, _ => LandmarkExists(path, landmark));
            if (exists)
            {
                landmarks.Add(landmark);
            }
        }

        return landmarks;
    }

    private static bool LandmarkExists(string path, string landmark)
    {
        try
        {
            var candidatePath = Path.Combine(path, landmark);

            if (Landmarks_c.FolderLandmarks.Contains(landmark))
            {
                return Directory.Exists(candidatePath);
            }

            if (landmark.StartsWith('.'))
            {
                return Directory.EnumerateFiles(path, $"*{landmark}", SearchOption.TopDirectoryOnly).Any();
            }

            return File.Exists(candidatePath);
        }
        catch
        {
            return false;
        }
    }
}
