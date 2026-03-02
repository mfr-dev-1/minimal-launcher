namespace Launcher.Core.Workflows;

public sealed class ToolSuggestionResolver_c
{
    public IReadOnlyList<string> ResolveCandidateToolIds(Launcher.Core.Models.ProjectRecord_c project, string fallbackToolId)
    {
        var landmarks = new HashSet<string>(project.Landmarks, StringComparer.OrdinalIgnoreCase);

        if (HasAny(landmarks, ".sln"))
        {
            return [Launcher.Core.Models.ToolIds_c.VisualStudio, Launcher.Core.Models.ToolIds_c.Rider, Launcher.Core.Models.ToolIds_c.VsCode];
        }

        if (landmarks.Contains(".idea"))
        {
            if (HasAny(landmarks, "pom.xml", "build.gradle", "settings.gradle"))
            {
                return [Launcher.Core.Models.ToolIds_c.IntelliJ, Launcher.Core.Models.ToolIds_c.VsCode];
            }

            if (HasAny(landmarks, "pyproject.toml", "requirements.txt"))
            {
                return [Launcher.Core.Models.ToolIds_c.PyCharm, Launcher.Core.Models.ToolIds_c.VsCode];
            }

            if (landmarks.Contains("package.json"))
            {
                return [Launcher.Core.Models.ToolIds_c.WebStorm, Launcher.Core.Models.ToolIds_c.VsCode];
            }
        }

        if (HasAny(landmarks, ".vscode", ".code-workspace"))
        {
            return [Launcher.Core.Models.ToolIds_c.VsCode];
        }

        if (landmarks.Contains("Cargo.toml"))
        {
            return [Launcher.Core.Models.ToolIds_c.RustRover, Launcher.Core.Models.ToolIds_c.VsCode];
        }

        if (landmarks.Contains("go.mod"))
        {
            return [Launcher.Core.Models.ToolIds_c.GoLand, Launcher.Core.Models.ToolIds_c.VsCode];
        }

        if (landmarks.Contains("composer.json"))
        {
            return [Launcher.Core.Models.ToolIds_c.PhpStorm, Launcher.Core.Models.ToolIds_c.VsCode];
        }

        return [fallbackToolId, Launcher.Core.Models.ToolIds_c.VsCode];
    }

    private static bool HasAny(HashSet<string> landmarks, params string[] items)
    {
        return items.Any(landmarks.Contains);
    }
}
