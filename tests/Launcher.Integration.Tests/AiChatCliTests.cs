using System.Diagnostics;

namespace Launcher.Integration.Tests;

public sealed class AiChatCli_c_Tests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "launcher-aichat-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ExecuteAsync_EmbeddedPromptTokenTemplate_IsRejected()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var cli = new Launcher.Infrastructure.Launch.AiChatCli_c();
        var settings = new Launcher.Core.Models.AiChatSettings_c
        {
            CliExecutable = "powershell.exe",
            ArgumentTemplate = "-NoLogo -NoProfile -Command \"Write-Output {prompt}\"",
            TimeoutSeconds = 30
        };

        var result = await cli.ExecuteAsync(settings, "hello", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("standalone tokens", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_LongRunningCommand_ReturnsTimeoutAndKillsProcess()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Directory.CreateDirectory(_tempRoot);
        var timeoutScriptPath = Path.Combine(_tempRoot, "timeout.ps1");
        File.WriteAllText(timeoutScriptPath, "Start-Sleep -Seconds 8");

        var cli = new Launcher.Infrastructure.Launch.AiChatCli_c();
        var settings = new Launcher.Core.Models.AiChatSettings_c
        {
            CliExecutable = "powershell.exe",
            ArgumentTemplate = $"-NoLogo -NoProfile -File \"{timeoutScriptPath}\" {{prompt}}",
            TimeoutSeconds = 1
        };

        var stopwatch = Stopwatch.StartNew();
        var result = await cli.ExecuteAsync(settings, "hello", CancellationToken.None);
        stopwatch.Stop();

        Assert.False(result.IsSuccess);
        Assert.Contains("timeout after 5 seconds", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(8));
    }

    [Fact]
    public async Task ExecuteAsync_StandalonePromptToken_DoesNotExecutePromptAsScript()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Directory.CreateDirectory(_tempRoot);
        var createdByInjectionPath = Path.Combine(_tempRoot, "injection.txt");
        var echoScriptPath = Path.Combine(_tempRoot, "echo.ps1");
        File.WriteAllText(echoScriptPath, "param([string]$Prompt)\nWrite-Output $Prompt");
        var cli = new Launcher.Infrastructure.Launch.AiChatCli_c();
        var settings = new Launcher.Core.Models.AiChatSettings_c
        {
            CliExecutable = "powershell.exe",
            ArgumentTemplate = $"-NoLogo -NoProfile -File \"{echoScriptPath}\" {{prompt}}",
            TimeoutSeconds = 30
        };
        var prompt = $"safe; New-Item -Path '{createdByInjectionPath}' -ItemType File -Force";

        var result = await cli.ExecuteAsync(settings, prompt, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("safe;", result.OutputMarkdown, StringComparison.Ordinal);
        Assert.False(File.Exists(createdByInjectionPath));
    }

    [Fact]
    public async Task ExecuteAsync_CliExecutableFromPath_ResolvesAndRunsCmdTool()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Directory.CreateDirectory(_tempRoot);
        var fakeCliPath = Path.Combine(_tempRoot, "fakeaicli.cmd");
        File.WriteAllText(fakeCliPath, "@echo off\r\necho %*");

        var previousPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", _tempRoot + ";" + previousPath);
        try
        {
            var cli = new Launcher.Infrastructure.Launch.AiChatCli_c();
            var settings = new Launcher.Core.Models.AiChatSettings_c
            {
                CliExecutable = "fakeaicli",
                ArgumentTemplate = "{prompt}",
                TimeoutSeconds = 30
            };

            var result = await cli.ExecuteAsync(settings, "hello from path", CancellationToken.None);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Contains("hello from path", result.OutputMarkdown, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", previousPath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitExtensionlessPath_PrefersCmdSibling()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Directory.CreateDirectory(_tempRoot);
        var fakeCliWithoutExtension = Path.Combine(_tempRoot, "explicitcli");
        var fakeCliCmdPath = fakeCliWithoutExtension + ".cmd";
        File.WriteAllText(fakeCliWithoutExtension, "not a windows executable");
        File.WriteAllText(fakeCliCmdPath, "@echo off\r\necho %*");

        var cli = new Launcher.Infrastructure.Launch.AiChatCli_c();
        var settings = new Launcher.Core.Models.AiChatSettings_c
        {
            CliExecutable = fakeCliWithoutExtension,
            ArgumentTemplate = "{prompt}",
            TimeoutSeconds = 30
        };

        var result = await cli.ExecuteAsync(settings, "hello explicit path", CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Contains("hello explicit path", result.OutputMarkdown, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Ignore.
        }
    }
}
