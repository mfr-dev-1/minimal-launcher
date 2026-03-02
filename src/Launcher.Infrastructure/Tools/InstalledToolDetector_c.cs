using System.Diagnostics;
using System.Runtime.Versioning;
using Launcher.Core.Models;
using Microsoft.Win32;

namespace Launcher.Infrastructure.Tools;

public sealed class InstalledToolDetector_c
{
    public ToolRecord_c DetectSingle(ToolSpec_c spec, LauncherSettings_c settings)
    {
        var overridePath = settings.ToolPaths
            .Where(x => string.Equals(x.ToolId, spec.ToolId, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Path)
            .FirstOrDefault(path => IsValidExecutable(path));

        if (overridePath is not null)
        {
            return Available(spec, overridePath);
        }

        if (spec.UseVsWhere)
        {
            var vsWhereDetected = ResolveVisualStudioViaVsWhere();
            if (vsWhereDetected is not null)
            {
                return Available(spec, vsWhereDetected);
            }
        }

        var fromPath = ResolveFromPath(spec.ExecutableCandidates);
        if (fromPath is not null)
        {
            return Available(spec, fromPath);
        }

        var fromKnownDirs = ResolveFromKnownDirectories(spec.ExecutableCandidates, spec.KnownDirectories);
        if (fromKnownDirs is not null)
        {
            return Available(spec, fromKnownDirs);
        }

        var fromRegistry = ResolveFromAppPaths(spec.ExecutableCandidates);
        if (fromRegistry is not null)
        {
            return Available(spec, fromRegistry);
        }

        return new ToolRecord_c
        {
            ToolId = spec.ToolId,
            DisplayName = spec.DisplayName,
            ExecutablePath = string.Empty,
            IsAvailable = false
        };
    }

    private static ToolRecord_c Available(ToolSpec_c spec, string executablePath)
    {
        return new ToolRecord_c
        {
            ToolId = spec.ToolId,
            DisplayName = spec.DisplayName,
            ExecutablePath = executablePath,
            IsAvailable = true
        };
    }

    private static string? ResolveVisualStudioViaVsWhere()
    {
        var pathCandidates = new[]
        {
            @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe",
            @"C:\Program Files\Microsoft Visual Studio\Installer\vswhere.exe"
        };

        var vsWhere = pathCandidates.FirstOrDefault(File.Exists);
        if (vsWhere is null)
        {
            return null;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = vsWhere,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-latest");
            psi.ArgumentList.Add("-requires");
            psi.ArgumentList.Add("Microsoft.Component.MSBuild");
            psi.ArgumentList.Add("-find");
            psi.ArgumentList.Add("Common7\\IDE\\devenv.exe");

            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(3000);
            return IsValidExecutable(output) ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveFromPath(IReadOnlyList<string> executableCandidates)
    {
        foreach (var executable in executableCandidates)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = executable,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process is null)
                {
                    continue;
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(1500);
                var first = output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                if (IsValidExecutable(first))
                {
                    return first;
                }
            }
            catch
            {
                // Ignore failures and continue.
            }
        }

        return null;
    }

    private static string? ResolveFromKnownDirectories(IReadOnlyList<string> executableCandidates, IReadOnlyList<string> knownDirectories)
    {
        foreach (var directory in knownDirectories)
        {
            var expanded = Environment.ExpandEnvironmentVariables(directory);
            if (!Directory.Exists(expanded))
            {
                continue;
            }

            foreach (var candidate in executableCandidates)
            {
                var full = Path.Combine(expanded, candidate);
                if (IsValidExecutable(full))
                {
                    return full;
                }

                try
                {
                    var nested = Directory.EnumerateFiles(expanded, candidate, SearchOption.AllDirectories).FirstOrDefault();
                    if (IsValidExecutable(nested))
                    {
                        return nested;
                    }
                }
                catch
                {
                    // Ignore inaccessible trees.
                }
            }
        }

        return null;
    }

    private static string? ResolveFromAppPaths(IReadOnlyList<string> executableCandidates)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        return ResolveFromAppPathsWindows(executableCandidates);
    }

    [SupportedOSPlatform("windows")]
    private static string? ResolveFromAppPathsWindows(IReadOnlyList<string> executableCandidates)
    {
        var hives = new[]
        {
            Registry.CurrentUser,
            Registry.LocalMachine
        };

        foreach (var hive in hives)
        {
            foreach (var executable in executableCandidates)
            {
                var keyPath = $"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\{executable}";
                using var key = hive.OpenSubKey(keyPath);
                var value = key?.GetValue(string.Empty) as string;
                if (IsValidExecutable(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static bool IsValidExecutable(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }
}
