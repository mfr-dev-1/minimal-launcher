using System.Text.Json;
using Launcher.Core.Models;

namespace Launcher.Infrastructure.Storage;

public sealed class PortableIndexStore_c
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _indexPath;

    public PortableIndexStore_c(string baseDirectory)
    {
        _indexPath = Path.Combine(baseDirectory, "launcher.index.json");
    }

    public ProjectIndexSnapshot_c Load()
    {
        if (!File.Exists(_indexPath))
        {
            return new ProjectIndexSnapshot_c();
        }

        var content = File.ReadAllText(_indexPath);
        return JsonSerializer.Deserialize<ProjectIndexSnapshot_c>(content, JsonOptions) ?? new ProjectIndexSnapshot_c();
    }

    public void Save(ProjectIndexSnapshot_c snapshot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_indexPath)!);
        var content = JsonSerializer.Serialize(snapshot, JsonOptions);
        File.WriteAllText(_indexPath, content);
    }
}
