using System.Diagnostics;

namespace Launcher.Infrastructure.Launch;

public sealed class ShellTerminal_c
{
    public async Task<Launcher.Application.Models.TerminalCommandResult_c> ExecuteAsync(
        Launcher.Core.Models.TerminalSettings_c settings,
        string commandText,
        CancellationToken cancellationToken)
    {
        var trimmedCommand = commandText?.Trim() ?? string.Empty;
        if (trimmedCommand.Length == 0)
        {
            return Launcher.Application.Models.TerminalCommandResult_c.Fail(trimmedCommand, "Terminal command is empty.");
        }

        var shellExecutable = string.IsNullOrWhiteSpace(settings.ShellExecutable)
            ? Launcher.Core.Models.TerminalSettings_c.CreateDefault().ShellExecutable
            : settings.ShellExecutable.Trim();

        var shellPrefix = settings.ShellArgumentsPrefix is { Count: > 0 }
            ? settings.ShellArgumentsPrefix
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
            : [];
        if (shellPrefix.Count == 0)
        {
            shellPrefix = Launcher.Core.Models.TerminalSettings_c.BuildDefaultShellArgumentPrefix_c();
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = shellExecutable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var token in shellPrefix)
        {
            startInfo.ArgumentList.Add(token);
        }

        startInfo.ArgumentList.Add(trimmedCommand);

        try
        {
            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;
            return Launcher.Application.Models.TerminalCommandResult_c.Success(
                trimmedCommand,
                standardOutput.Trim(),
                standardError.Trim(),
                process.ExitCode);
        }
        catch (Exception ex)
        {
            return Launcher.Application.Models.TerminalCommandResult_c.Fail(trimmedCommand, $"Terminal execution failed: {ex.Message}");
        }
    }
}
