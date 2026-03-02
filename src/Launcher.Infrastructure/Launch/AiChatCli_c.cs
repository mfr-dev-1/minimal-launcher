using System.Diagnostics;

namespace Launcher.Infrastructure.Launch;

public sealed class AiChatCli_c
{
    public async Task<Launcher.Application.Models.AiChatResult_c> ExecuteAsync(
        Launcher.Core.Models.AiChatSettings_c settings,
        string prompt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return Launcher.Application.Models.AiChatResult_c.Fail("Prompt is empty.");
        }

        if (string.IsNullOrWhiteSpace(settings.CliExecutable))
        {
            return Launcher.Application.Models.AiChatResult_c.Fail("AI CLI executable is not configured. Check your 'Settings'.");
        }

        var argumentTemplate = string.IsNullOrWhiteSpace(settings.ArgumentTemplate)
            ? "--prompt {prompt}"
            : settings.ArgumentTemplate;

        var tokens = CommandArgumentTemplate_c.Tokenize(argumentTemplate);
        if (!tokens.Any(x => x.Contains("{prompt}", StringComparison.Ordinal)))
        {
            tokens.Add("{prompt}");
        }

        if (HasEmbeddedPromptOrContextToken_c(tokens))
        {
            return Launcher.Application.Models.AiChatResult_c.Fail("AI argument template must pass {prompt} and {context} as standalone tokens.");
        }

        if (UsesShellEvalSwitchWithPrompt_c(tokens))
        {
            return Launcher.Application.Models.AiChatResult_c.Fail("AI argument template cannot use shell-eval switches when passing {prompt}.");
        }

        var resolvedContextDirectory = ResolveContextDirectory_c(settings.ContextDirectory);
        var resolvedArguments = tokens
            .Select(x => x.Replace("{prompt}", prompt, StringComparison.Ordinal))
            .Select(x => x.Replace("{context}", resolvedContextDirectory ?? string.Empty, StringComparison.Ordinal))
            .ToList();

        if (!TryBuildCliStartCommand_c(
                settings.CliExecutable.Trim(),
                resolvedArguments,
                out var commandFileName,
                out var commandArguments,
                out var rawArgumentLine,
                out var resolveError))
        {
            return Launcher.Application.Models.AiChatResult_c.Fail(resolveError ?? "AI CLI executable is not available.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = commandFileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(rawArgumentLine))
        {
            startInfo.Arguments = rawArgumentLine;
        }

        if (!string.IsNullOrWhiteSpace(resolvedContextDirectory))
        {
            startInfo.WorkingDirectory = resolvedContextDirectory;
        }

        foreach (var argument in commandArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            var timeout = TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 5, 300));
            var timeoutTask = Task.Delay(timeout, cancellationToken);
            var exitTask = process.WaitForExitAsync(cancellationToken);
            var finishedTask = await Task.WhenAny(exitTask, timeoutTask);

            if (finishedTask == timeoutTask)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Ignore best-effort kill failures.
                }

                return Launcher.Application.Models.AiChatResult_c.Fail($"AI CLI timeout after {timeout.TotalSeconds:0} seconds.");
            }

            await exitTask;

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(standardOutput))
            {
                var error = string.IsNullOrWhiteSpace(standardError)
                    ? $"AI CLI exited with {process.ExitCode}."
                    : standardError.Trim();
                return Launcher.Application.Models.AiChatResult_c.Fail(error);
            }

            var markdownOutput = string.IsNullOrWhiteSpace(standardOutput)
                ? standardError.Trim()
                : standardOutput.Trim();

            return Launcher.Application.Models.AiChatResult_c.Success(markdownOutput);
        }
        catch (Exception ex)
        {
            return Launcher.Application.Models.AiChatResult_c.Fail($"AI CLI execution failed: {ex.Message}");
        }
    }

    private static bool TryBuildCliStartCommand_c(
        string rawExecutable,
        IReadOnlyList<string> resolvedArguments,
        out string commandFileName,
        out IReadOnlyList<string> commandArguments,
        out string? rawArgumentLine,
        out string? error)
    {
        commandFileName = string.Empty;
        commandArguments = [];
        rawArgumentLine = null;
        error = null;

        var resolvedExecutablePath = ResolveExecutablePath_c(rawExecutable);
        if (string.IsNullOrWhiteSpace(resolvedExecutablePath))
        {
            error = $"AI CLI executable '{rawExecutable}' was not found. Use full path or ensure it is on PATH.";
            return false;
        }

        if (OperatingSystem.IsWindows() && RequiresCommandInterpreter_c(resolvedExecutablePath))
        {
            var comSpec = Environment.GetEnvironmentVariable("ComSpec");
            if (string.IsNullOrWhiteSpace(comSpec))
            {
                comSpec = "cmd.exe";
            }

            commandFileName = comSpec;
            commandArguments = [];
            rawArgumentLine = BuildCmdArgumentLine_c(resolvedExecutablePath, resolvedArguments);
            return true;
        }

        commandFileName = resolvedExecutablePath;
        commandArguments = resolvedArguments.ToList();
        return true;
    }

    private static string? ResolveExecutablePath_c(string rawExecutable)
    {
        if (string.IsNullOrWhiteSpace(rawExecutable))
        {
            return null;
        }

        if (TryResolveExplicitPath_c(rawExecutable, out var explicitPath))
        {
            return PreferRunnableWindowsExecutablePath_c(explicitPath);
        }

        if (OperatingSystem.IsWindows())
        {
            var fromWhere = ResolveFromWhere_c(rawExecutable);
            if (!string.IsNullOrWhiteSpace(fromWhere))
            {
                return PreferRunnableWindowsExecutablePath_c(fromWhere);
            }
        }

        return PreferRunnableWindowsExecutablePath_c(ResolveFromPathEnvironment_c(rawExecutable));
    }

    private static bool TryResolveExplicitPath_c(string rawExecutable, out string resolvedPath)
    {
        resolvedPath = string.Empty;

        var hasDirectorySeparator = rawExecutable.Contains(Path.DirectorySeparatorChar) || rawExecutable.Contains(Path.AltDirectorySeparatorChar);
        if (!Path.IsPathRooted(rawExecutable) && !hasDirectorySeparator)
        {
            return false;
        }

        var fullPath = Path.GetFullPath(rawExecutable);
        if (!Path.HasExtension(fullPath))
        {
            foreach (var extension in GetExecutableExtensions_c())
            {
                var candidate = fullPath + extension;
                if (File.Exists(candidate))
                {
                    resolvedPath = candidate;
                    return true;
                }
            }
        }

        if (File.Exists(fullPath))
        {
            resolvedPath = fullPath;
            return true;
        }

        if (Path.HasExtension(fullPath))
        {
            return false;
        }

        foreach (var extension in GetExecutableExtensions_c())
        {
            var candidate = fullPath + extension;
            if (File.Exists(candidate))
            {
                resolvedPath = candidate;
                return true;
            }
        }

        return false;
    }

    private static string? ResolveFromWhere_c(string executable)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1500);
            if (process.ExitCode != 0)
            {
                return null;
            }

            var lines = output
                .Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(File.Exists)
                .ToList();
            if (lines.Count == 0)
            {
                return null;
            }

            return lines
                .OrderBy(ScoreWindowsExecutableCandidate_c)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveFromPathEnvironment_c(string executable)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        var candidates = BuildExecutableCandidates_c(executable);
        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            string expandedDirectory;
            try
            {
                expandedDirectory = Environment.ExpandEnvironmentVariables(directory);
            }
            catch
            {
                continue;
            }

            foreach (var candidateName in candidates)
            {
                var candidatePath = Path.Combine(expandedDirectory, candidateName);
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }
        }

        return null;
    }

    private static List<string> BuildExecutableCandidates_c(string executable)
    {
        var candidates = new List<string>();
        if (Path.HasExtension(executable))
        {
            candidates.Add(executable);
            return candidates;
        }

        var executableExtensions = GetExecutableExtensions_c();
        foreach (var extension in executableExtensions)
        {
            candidates.Add(executable + extension);
        }

        // Keep bare name as fallback for non-standard extensionless binaries.
        candidates.Add(executable);

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> GetExecutableExtensions_c()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [string.Empty];
        }

        var pathExt = Environment.GetEnvironmentVariable("PATHEXT");
        if (string.IsNullOrWhiteSpace(pathExt))
        {
            return [".EXE", ".COM", ".BAT", ".CMD"];
        }

        var parsed = pathExt
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.StartsWith(".", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return parsed.Count == 0 ? [".EXE", ".COM", ".BAT", ".CMD"] : parsed;
    }

    private static bool RequiresCommandInterpreter_c(string executablePath)
    {
        var extension = Path.GetExtension(executablePath);
        return extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bat", StringComparison.OrdinalIgnoreCase);
    }

    private static int ScoreWindowsExecutableCandidate_c(string path)
    {
        var extension = Path.GetExtension(path);
        if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (extension.Equals(".com", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (extension.Equals(".bat", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (string.IsNullOrWhiteSpace(extension))
        {
            return 10;
        }

        return 20;
    }

    private static string? PreferRunnableWindowsExecutablePath_c(string? path)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var extension = Path.GetExtension(path);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return path;
        }

        foreach (var executableExtension in GetExecutableExtensions_c())
        {
            var candidate = path + executableExtension;
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return path;
    }

    private static string BuildCmdArgumentLine_c(string executablePath, IReadOnlyList<string> arguments)
    {
        var invocation = BuildCmdInvocation_c(executablePath, arguments);
        return $"/d /s /c \"{invocation}\"";
    }

    private static string BuildCmdInvocation_c(string executablePath, IReadOnlyList<string> arguments)
    {
        var tokens = new List<string> { EscapeForCmdQuotedToken_c(executablePath) };
        tokens.AddRange(arguments.Select(EscapeForCmdQuotedToken_c));
        return string.Join(" ", tokens);
    }

    private static string EscapeForCmdQuotedToken_c(string token)
    {
        var escaped = token
            .Replace("^", "^^", StringComparison.Ordinal)
            .Replace("&", "^&", StringComparison.Ordinal)
            .Replace("|", "^|", StringComparison.Ordinal)
            .Replace("<", "^<", StringComparison.Ordinal)
            .Replace(">", "^>", StringComparison.Ordinal)
            .Replace("(", "^(", StringComparison.Ordinal)
            .Replace(")", "^)", StringComparison.Ordinal)
            .Replace("%", "%%", StringComparison.Ordinal)
            .Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }

    private static string? ResolveContextDirectory_c(string contextDirectory)
    {
        if (string.IsNullOrWhiteSpace(contextDirectory))
        {
            return null;
        }

        try
        {
            var full = Path.GetFullPath(contextDirectory);
            return Directory.Exists(full) ? full : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool HasEmbeddedPromptOrContextToken_c(IReadOnlyList<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (token.Contains("{prompt}", StringComparison.Ordinal) &&
                !string.Equals(token, "{prompt}", StringComparison.Ordinal))
            {
                return true;
            }

            if (token.Contains("{context}", StringComparison.Ordinal) &&
                !string.Equals(token, "{context}", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool UsesShellEvalSwitchWithPrompt_c(IReadOnlyList<string> tokens)
    {
        var hasPromptPlaceholder = tokens.Any(x => x.Contains("{prompt}", StringComparison.Ordinal));
        if (!hasPromptPlaceholder)
        {
            return false;
        }

        return tokens.Any(x =>
            string.Equals(x, "-Command", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x, "-EncodedCommand", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x, "/C", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x, "/K", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x, "-c", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x, "-lc", StringComparison.OrdinalIgnoreCase));
    }
}
