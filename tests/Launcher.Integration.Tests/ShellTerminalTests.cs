namespace Launcher.Integration.Tests;

public sealed class ShellTerminal_c_Tests
{
    [Fact]
    public async Task ExecuteAsync_WhitespacePrefix_FallsBackToDefaultPrefix()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var terminal = new Launcher.Infrastructure.Launch.ShellTerminal_c();
        var settings = new Launcher.Core.Models.TerminalSettings_c
        {
            ShellExecutable = "powershell.exe",
            ShellArgumentsPrefix = [" ", "   "]
        };

        var result = await terminal.ExecuteAsync(settings, "Write-Output launcher", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("launcher", result.OutputText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidShellExecutable_ReturnsFailure()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var terminal = new Launcher.Infrastructure.Launch.ShellTerminal_c();
        var settings = new Launcher.Core.Models.TerminalSettings_c
        {
            ShellExecutable = @"C:\__launcher_missing_shell__\none.exe",
            ShellArgumentsPrefix = Launcher.Core.Models.TerminalSettings_c.BuildDefaultShellArgumentPrefix_c()
        };

        var result = await terminal.ExecuteAsync(settings, "Write-Output launcher", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Terminal execution failed", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_StandardError_IsReturnedSeparately()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var terminal = new Launcher.Infrastructure.Launch.ShellTerminal_c();
        var settings = new Launcher.Core.Models.TerminalSettings_c
        {
            ShellExecutable = "powershell.exe",
            ShellArgumentsPrefix = Launcher.Core.Models.TerminalSettings_c.BuildDefaultShellArgumentPrefix_c()
        };

        var result = await terminal.ExecuteAsync(settings, "[Console]::Error.WriteLine('launcher-stderr')", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("launcher-stderr", result.StandardErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("(stderr)", result.OutputText, StringComparison.OrdinalIgnoreCase);
    }
}
