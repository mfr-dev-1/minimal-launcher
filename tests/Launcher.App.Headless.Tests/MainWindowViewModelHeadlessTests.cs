using Avalonia.Headless.XUnit;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Launcher.Application.Ports;
using Launcher.App.ViewModels;

namespace Launcher.App.Headless.Tests;

public sealed class MainWindowViewModelHeadlessTests
{
    [AvaloniaFact]
    public async Task SearchDebounce_UpdatesDisplayRows()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SearchText = "launch";
        await Task.Delay(280);

        Assert.NotEmpty(viewModel.DisplayRows);
    }

    [AvaloniaFact]
    public async Task MoveSelection_UpdatesIndex()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SelectedIndex = 0;
        viewModel.MoveSelection(1);

        Assert.Equal(1, viewModel.SelectedIndex);
    }

    [AvaloniaFact]
    public async Task EnterOnSelectedProject_HidesOrEntersFilePickerForEditorTools()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        var hideRequested = false;
        viewModel.RequestHideLauncher += (_, _) => hideRequested = true;

        await viewModel.HandleEnterAsync();

        Assert.True(hideRequested || viewModel.IsFilePickerMode);
    }

    [AvaloniaFact]
    public async Task SwitchToNextMode_CyclesProjectAiTerminalMetaProject()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SwitchToNextMode();
        Assert.True(viewModel.IsAiChatMode);
        Assert.Equal("?", viewModel.SearchText);

        viewModel.SwitchToNextMode();
        Assert.True(viewModel.IsTerminalMode);
        Assert.Equal(">", viewModel.SearchText);

        viewModel.SwitchToNextMode();
        await Task.Delay(60);
        Assert.True(viewModel.IsProjectSearchMode);
        Assert.True(viewModel.IsMetaCommandMode);
        Assert.Equal(">>", viewModel.SearchText);

        viewModel.SwitchToNextMode();
        await Task.Delay(60);
        Assert.True(viewModel.IsProjectSearchMode);
        Assert.False(viewModel.IsMetaCommandMode);
        Assert.Equal(string.Empty, viewModel.SearchText);
    }

    [AvaloniaFact]
    public async Task SwitchToNextMode_FilePickerNotIndependent_JumpsToAi()
    {
        var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
        settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Completed;
        settings.Defaults.FallbackToolId = Launcher.Core.Models.ToolIds_c.VsCode;

        var fixture = new ViewModelFixture_c(settings);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        var handled = await viewModel.TryHandleAlternativeLaunchAsync(Key.Z, KeyModifiers.Shift | KeyModifiers.Alt);
        await Task.Delay(80);
        Assert.True(handled);
        Assert.True(viewModel.IsFilePickerMode);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        Assert.True(viewModel.IsAiChatMode);
        Assert.Equal("?", viewModel.SearchText);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        Assert.True(viewModel.IsTerminalMode);
        Assert.Equal(">", viewModel.SearchText);

        viewModel.SwitchToNextMode();
        await Task.Delay(60);
        Assert.True(viewModel.IsProjectSearchMode);
        Assert.True(viewModel.IsMetaCommandMode);
        Assert.Equal(">>", viewModel.SearchText);
    }

    [AvaloniaFact]
    public async Task SearchPrefixQuestion_SwitchesToAiModeAndStripsPrefix()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SearchText = "   ?  hello ai";
        await Task.Delay(60);

        Assert.True(viewModel.IsAiChatMode);
        Assert.Equal("hello ai", viewModel.SearchText);
    }

    [AvaloniaFact]
    public async Task SwitchToNextMode_UpdatesCommandHintAndWatermark()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Contains("projects", viewModel.CommandHintText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Search projects", viewModel.SearchWatermarkText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(">", viewModel.SearchPrefixText);

        viewModel.SwitchToNextMode();
        Assert.Contains("ai", viewModel.CommandHintText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ask AI", viewModel.SearchWatermarkText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("?", viewModel.SearchPrefixText);

        viewModel.SwitchToNextMode();
        Assert.Contains("terminal", viewModel.CommandHintText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("> Type command", viewModel.SearchWatermarkText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(">", viewModel.SearchPrefixText);

        viewModel.SwitchToNextMode();
        await Task.Delay(60);
        Assert.True(viewModel.IsMetaCommandMode);
        Assert.Contains("projects", viewModel.CommandHintText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(">>", viewModel.SearchPrefixText);
    }

    [AvaloniaFact]
    public async Task TopHintNavigation_TransitionsModesAndMetaCycle()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        await viewModel.NavigateToTerminalModeAsync();
        Assert.True(viewModel.IsTerminalMode);
        Assert.Equal(">", viewModel.SearchText);

        viewModel.NavigateToMetaMode();
        await Task.Delay(60);
        Assert.True(viewModel.IsProjectSearchMode);
        Assert.True(viewModel.IsMetaCommandMode);
        Assert.Equal(">>", viewModel.SearchText);

        await viewModel.NavigateToProjectModeAsync();
        await Task.Delay(60);
        Assert.True(viewModel.IsProjectSearchMode);
        Assert.False(viewModel.IsMetaCommandMode);
        Assert.Equal(string.Empty, viewModel.SearchText);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        Assert.True(viewModel.IsAiChatMode);
        viewModel.SearchText = "keep me";

        await viewModel.NavigateToProjectModeAsync();
        await Task.Delay(60);
        Assert.True(viewModel.IsProjectSearchMode);
        Assert.False(viewModel.IsAiChatMode);
        Assert.Equal(string.Empty, viewModel.SearchText);
    }

    [AvaloniaFact]
    public async Task SwitchToNextMode_AiInputLayoutAndStatelessResetBehaviors()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(52d, viewModel.SearchInputHostHeight);
        Assert.Equal(TextWrapping.NoWrap, viewModel.SearchInputTextWrapping);
        Assert.Equal(1, viewModel.SearchInputMinLines);
        Assert.Equal(1, viewModel.SearchInputMaxLines);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        Assert.True(viewModel.IsAiChatMode);
        Assert.Equal(104d, viewModel.SearchInputHostHeight);
        Assert.Equal(TextWrapping.Wrap, viewModel.SearchInputTextWrapping);
        Assert.Equal(3, viewModel.SearchInputMinLines);
        Assert.Equal(3, viewModel.SearchInputMaxLines);

        viewModel.SearchText = "retained prompt";
        viewModel.AiMarkdownBuilder.Append("retained response");
        Assert.Contains("retained response", viewModel.AiMarkdownBuilder.ToString(), StringComparison.Ordinal);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        Assert.True(viewModel.IsTerminalMode);
        Assert.Equal(52d, viewModel.SearchInputHostHeight);
        Assert.Equal(TextWrapping.NoWrap, viewModel.SearchInputTextWrapping);
        Assert.Equal(1, viewModel.SearchInputMinLines);
        Assert.Equal(1, viewModel.SearchInputMaxLines);
        Assert.Equal(string.Empty, viewModel.AiMarkdownBuilder.ToString());

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        Assert.True(viewModel.IsProjectSearchMode);
        Assert.True(viewModel.IsMetaCommandMode);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        Assert.True(viewModel.IsProjectSearchMode);
        Assert.False(viewModel.IsMetaCommandMode);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        Assert.True(viewModel.IsAiChatMode);
        Assert.Equal("?", viewModel.SearchText);
        Assert.Equal(string.Empty, viewModel.AiMarkdownBuilder.ToString());
    }

    [AvaloniaFact]
    public async Task FooterVisibility_TracksModesAndMeta()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.True(viewModel.IsFooterVisible);
        Assert.True(viewModel.IsFooterIdeOptionsVisible);
        Assert.True(viewModel.IsFooterStatusVisible);
        Assert.False(viewModel.IsFooterMetaHintVisible);
        Assert.False(viewModel.IsFooterAiHintVisible);
        Assert.False(viewModel.IsFooterTerminalHintVisible);
        Assert.False(viewModel.IsFooterFilePickerHintVisible);

        viewModel.SearchText = ">> settings";
        await Task.Delay(280);

        Assert.True(viewModel.IsFooterMetaHintVisible);
        Assert.False(viewModel.IsFooterIdeOptionsVisible);
        Assert.False(viewModel.IsFooterAiHintVisible);
        Assert.False(viewModel.IsFooterTerminalHintVisible);
        Assert.False(viewModel.IsFooterFilePickerHintVisible);

        viewModel.SwitchToNextMode();
        Assert.True(viewModel.IsFooterIdeOptionsVisible);
        Assert.False(viewModel.IsFooterMetaHintVisible);

        viewModel.SwitchToNextMode();
        Assert.True(viewModel.IsFooterAiHintVisible);
        Assert.False(viewModel.IsFooterMetaHintVisible);
        Assert.False(viewModel.IsFooterIdeOptionsVisible);

        viewModel.SwitchToNextMode();
        Assert.True(viewModel.IsFooterTerminalHintVisible);
        Assert.False(viewModel.IsFooterAiHintVisible);
        Assert.False(viewModel.IsFooterMetaHintVisible);
    }

    [AvaloniaFact]
    public async Task AiMode_ProcessingBlocksResponseViewState()
    {
        var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
        settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Completed;

        var fixture = new ViewModelFixture_c(settings, new SlowAiChatPort_c(delayMs: 150));
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        Assert.True(viewModel.IsAiChatMode);
        Assert.False(viewModel.IsAiResponseBlocked);

        viewModel.SearchText = "hello";
        var sendTask = viewModel.HandleEnterAsync();
        await Task.Delay(30);
        Assert.True(viewModel.IsAiResponseBlocked);

        await sendTask;
        Assert.False(viewModel.IsAiResponseBlocked);
    }

    [AvaloniaFact]
    public async Task FooterVisibility_FilePickerAndSettingsHideBehaviors()
    {
        var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
        settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Completed;
        settings.Defaults.FallbackToolId = Launcher.Core.Models.ToolIds_c.VsCode;

        var fixture = new ViewModelFixture_c(settings);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        var handled = await viewModel.TryHandleAlternativeLaunchAsync(Key.Z, KeyModifiers.Shift | KeyModifiers.Alt);
        await Task.Delay(60);

        Assert.True(handled);
        Assert.True(viewModel.IsFilePickerMode);
        Assert.True(viewModel.IsFooterFilePickerHintVisible);
        Assert.False(viewModel.IsFooterIdeOptionsVisible);
        Assert.Equal("file picker | choose the specific file to open", viewModel.CommandHintText);

        viewModel.OpenSettings();
        Assert.False(viewModel.IsFooterVisible);
        Assert.False(viewModel.IsFooterStatusVisible);
        Assert.False(viewModel.IsFooterFilePickerHintVisible);
    }

    [AvaloniaFact]
    public async Task FilePickerSearch_FiltersFilesAndPrioritizesKnownTextExtensions()
    {
        var tempProjectPath = Path.Combine(Path.GetTempPath(), $"launcher-filepicker-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempProjectPath);
        Directory.CreateDirectory(Path.Combine(tempProjectPath, "src"));

        await File.WriteAllTextAsync(Path.Combine(tempProjectPath, "README.md"), "# launcher");
        await File.WriteAllTextAsync(Path.Combine(tempProjectPath, "readme.txt"), "notes");
        await File.WriteAllTextAsync(Path.Combine(tempProjectPath, "src", "ReadmeHelper.cs"), "namespace Temp;");
        await File.WriteAllBytesAsync(Path.Combine(tempProjectPath, "readme.bin"), [0x01, 0x02, 0x03]);
        await File.WriteAllBytesAsync(Path.Combine(tempProjectPath, "readme.png"), [0x89, 0x50, 0x4E, 0x47]);

        try
        {
            var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
            settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Completed;
            settings.Defaults.FallbackToolId = Launcher.Core.Models.ToolIds_c.VsCode;
            settings.Defaults.ToolPriorityOrder = [Launcher.Core.Models.ToolIds_c.VsCode];
            settings.Search.DebounceMs = 15;

            var fixture = new ViewModelFixture_c(settings);
            fixture.EmitProjectsUpdated(
            [
                new Launcher.Core.Models.ProjectRecord_c
                {
                    ProjectName = "temp",
                    ProjectPath = tempProjectPath,
                    Landmarks = ["README.md"]
                }
            ]);

            using var viewModel = fixture.CreateViewModel();
            await viewModel.InitializeAsync(CancellationToken.None);

            await viewModel.HandleEnterAsync();
            await Task.Delay(180);

            Assert.True(viewModel.IsFilePickerMode);
            Assert.True(viewModel.IsSearchEnabled);

            viewModel.SearchText = "readme";
            await Task.Delay(140);

            var readmeRows = viewModel.DisplayRows
                .Where(row => row.RowKind == Launcher.App.Models.DisplayRowKind_c.FilePicker)
                .ToList();

            var markdownIndex = readmeRows.FindIndex(row => row.ProjectIdentifier == "README.md");
            var textIndex = readmeRows.FindIndex(row => row.ProjectIdentifier == "readme.txt");
            var binaryIndex = readmeRows.FindIndex(row => row.ProjectIdentifier == "readme.bin");
            var imageIndex = readmeRows.FindIndex(row => row.ProjectIdentifier == "readme.png");

            Assert.True(markdownIndex >= 0);
            Assert.True(textIndex >= 0);
            Assert.True(binaryIndex >= 0);
            Assert.True(imageIndex >= 0);
            Assert.True(markdownIndex < binaryIndex);
            Assert.True(markdownIndex < imageIndex);
            Assert.True(textIndex < binaryIndex);
            Assert.True(textIndex < imageIndex);

            viewModel.SearchText = "helper";
            await Task.Delay(140);

            var helperRows = viewModel.DisplayRows
                .Where(row => row.RowKind == Launcher.App.Models.DisplayRowKind_c.FilePicker)
                .ToList();

            Assert.Single(helperRows);
            Assert.Equal("ReadmeHelper.cs", helperRows[0].ProjectIdentifier);
        }
        finally
        {
            if (Directory.Exists(tempProjectPath))
            {
                Directory.Delete(tempProjectPath, recursive: true);
            }
        }
    }

    [AvaloniaFact]
    public async Task EnterOnMetaOnboarding_OpensOnboarding()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SearchText = ">> onboarding";
        await Task.Delay(280);
        await viewModel.HandleEnterAsync();

        Assert.True(viewModel.IsOnboardingVisible);
    }

    [AvaloniaFact]
    public async Task EnterOnMetaSettings_OpensSettings()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SearchText = ">> settings";
        await Task.Delay(280);
        await viewModel.HandleEnterAsync();

        Assert.True(viewModel.IsSettingsVisible);
    }

    [AvaloniaFact]
    public async Task OnboardingFinish_CompletesState()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Pending);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.OnboardingCustomRootText = Path.GetTempPath();
        viewModel.CompleteOnboarding();

        Assert.False(viewModel.IsOnboardingVisible);
        Assert.Equal(Launcher.Core.Models.OnboardingStates_c.Completed, fixture.Application.Settings.Onboarding.State);
    }

    [AvaloniaFact]
    public async Task OnboardingExit_DefaultsPendingState()
    {
        var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
        settings.Hotkeys.Toggle = string.Empty;
        settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Pending;

        var fixture = new ViewModelFixture_c(settings);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.HandleOnboardingExit();

        Assert.False(viewModel.IsOnboardingVisible);
        Assert.Equal(Launcher.Core.Models.OnboardingStates_c.Defaulted, fixture.Application.Settings.Onboarding.State);
        Assert.False(string.IsNullOrWhiteSpace(fixture.Application.Settings.Hotkeys.Toggle));
    }

    [AvaloniaFact]
    public async Task IndexUpdate_DoesNotForceDisplayRefresh()
    {
        var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
        settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Completed;
        settings.Search.DebounceMs = 20;

        var fixture = new ViewModelFixture_c(settings);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SearchText = string.Empty;
        await Task.Delay(120);

        var initialRows = viewModel.DisplayRows.Count;
        fixture.EmitProjectsUpdated(
        [
            new Launcher.Core.Models.ProjectRecord_c { ProjectName = "launcher", ProjectPath = "C:\\launcher", Landmarks = ["AGENTS.md"] },
            new Launcher.Core.Models.ProjectRecord_c { ProjectName = "library", ProjectPath = "C:\\library", Landmarks = ["README.md"] },
            new Launcher.Core.Models.ProjectRecord_c { ProjectName = "zeta", ProjectPath = "C:\\zeta", Landmarks = ["README.md"] }
        ]);

        await Task.Delay(160);

        Assert.Equal(initialRows, viewModel.DisplayRows.Count);
        Assert.DoesNotContain(viewModel.DisplayRows, row => string.Equals(row.ProjectIdentifier, "zeta", StringComparison.OrdinalIgnoreCase));
    }

    [AvaloniaFact]
    public async Task SettingsSave_ValidDraft_PersistsAndClosesOverlay()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.OpenSettings();
        viewModel.SettingsGeneralExitText = "Escape";
        viewModel.SettingsGeneralMoveUpText = "Up";
        viewModel.SettingsGeneralMoveDownText = "Down";
        viewModel.SettingsGeneralConfirmText = "Enter";
        viewModel.SettingsGeneralSwitchModeText = "Shift+Tab";
        viewModel.SettingsGeneralAlternativeModifiersText = "Shift+Alt";
        viewModel.SettingsGeneralAlternativeKeysText = "Z, X, C";
        viewModel.SettingsThemeOverrideText = Launcher.Core.Models.ThemeOverrideOptions_c.Light;
        viewModel.SettingsTerminalShellExecutableText = "powershell.exe";
        viewModel.SettingsTerminalShellArgumentsPrefixText = "-NoLogo -NoProfile -Command";
        viewModel.SettingsAiChatCliExecutableText = "claude.exe";
        viewModel.SettingsAiChatArgumentTemplateText = "--prompt {prompt}";
        viewModel.SettingsAiChatContextDirectoryText = string.Empty;
        viewModel.SettingsAiChatTimeoutSecondsText = "60";

        viewModel.SaveSettings();

        Assert.False(viewModel.IsSettingsVisible);
        Assert.Equal("powershell.exe", fixture.Application.Settings.Terminal.ShellExecutable);
        Assert.Equal("claude.exe", fixture.Application.Settings.AiChat.CliExecutable);
        Assert.Equal(60, fixture.Application.Settings.AiChat.TimeoutSeconds);
        Assert.Equal(Launcher.Core.Models.ThemeOverrideOptions_c.Light, fixture.Application.Settings.ThemeOverride);
        Assert.Equal(["Z", "X", "C"], fixture.Application.Settings.GeneralHotkeys.AlternativeLaunchKeys);
    }

    [AvaloniaFact]
    public async Task SettingsSave_UpdatesAlternativeModifierLabel()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.OpenSettings();
        viewModel.SettingsGeneralExitText = "Escape";
        viewModel.SettingsGeneralMoveUpText = "Up";
        viewModel.SettingsGeneralMoveDownText = "Down";
        viewModel.SettingsGeneralConfirmText = "Enter";
        viewModel.SettingsGeneralSwitchModeText = "Shift+Tab";
        viewModel.SettingsGeneralAlternativeModifiersText = "Ctrl+Alt";
        viewModel.SettingsGeneralAlternativeKeysText = "Z, X, C";
        viewModel.SettingsTerminalShellExecutableText = "powershell.exe";
        viewModel.SettingsTerminalShellArgumentsPrefixText = "-NoLogo -NoProfile -Command";
        viewModel.SettingsAiChatArgumentTemplateText = "--prompt {prompt}";
        viewModel.SettingsAiChatTimeoutSecondsText = "60";
        viewModel.SaveSettings();

        Assert.Equal("Ctrl+Alt", viewModel.AlternativeLaunchModifierLabelText);
    }

    [AvaloniaFact]
    public async Task SettingsSave_InvalidDraft_ShowsValidationError()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.OpenSettings();
        viewModel.SettingsGeneralConfirmText = "NotAKey";
        viewModel.SaveSettings();

        Assert.True(viewModel.IsSettingsVisible);
        Assert.True(viewModel.HasSettingsError);
    }

    [AvaloniaFact]
    public async Task OpenSettings_RaisesPrimaryFocusRequest()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        var focusRequested = false;
        viewModel.RequestFocusSettingsPrimary += (_, _) => focusRequested = true;
        viewModel.OpenSettings();

        Assert.True(focusRequested);
    }

    [AvaloniaFact]
    public async Task TerminalMode_RendersTerminalRowKinds()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SwitchToNextMode();
        viewModel.SwitchToNextMode();
        await Task.Delay(280);

        Assert.True(viewModel.IsTerminalMode);
        Assert.NotEmpty(viewModel.DisplayRows);
        Assert.All(viewModel.DisplayRows, row =>
        {
            Assert.Equal(Launcher.App.Models.DisplayRowKind_c.Terminal, row.RowKind);
            Assert.True(row.IsTerminalLikeRow);
            Assert.False(row.IsStructuredRow);
        });
    }

    [AvaloniaFact]
    public async Task TerminalPrefixSwitch_DoesNotShowProjectRowsDuringDebounceWindow()
    {
        var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
        settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Completed;
        settings.Search.DebounceMs = 200;

        var fixture = new ViewModelFixture_c(settings);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SearchText = "launch";
        await Task.Delay(240);
        Assert.Contains(viewModel.DisplayRows, row => row.RowKind == Launcher.App.Models.DisplayRowKind_c.Project);

        viewModel.SearchText = ">";
        await Task.Delay(40);

        Assert.True(viewModel.IsTerminalMode);
        Assert.NotEmpty(viewModel.DisplayRows);
        Assert.DoesNotContain(viewModel.DisplayRows, row => row.RowKind == Launcher.App.Models.DisplayRowKind_c.Project);
        Assert.All(viewModel.DisplayRows, row => Assert.True(row.IsTerminalLikeRow));
    }

    [AvaloniaFact]
    public async Task TerminalMode_ErrorOutput_RendersTerminalErrorRowKind()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        var terminalResult = await fixture.Application.ExecuteTerminalCommandAsync(">echo launcher", CancellationToken.None);
        Assert.False(terminalResult.IsSuccess);

        viewModel.SwitchToNextMode();
        viewModel.SwitchToNextMode();
        await Task.Delay(280);

        Assert.True(viewModel.IsTerminalMode);
        Assert.Contains(viewModel.DisplayRows, row =>
            row.RowKind == Launcher.App.Models.DisplayRowKind_c.TerminalError &&
            row.IsTerminalLikeRow &&
            row.IsTerminalErrorRow &&
            row.ProjectIdentifier.StartsWith("(error)", StringComparison.OrdinalIgnoreCase));
    }

    [AvaloniaFact]
    public async Task MainWindow_ExposesPrimaryAccessibleControls()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        var window = new Launcher.App.MainWindow(viewModel);
        await window.InitializeAsync(CancellationToken.None);

        var searchBox = window.FindControl<TextBox>("SearchBox");
        var searchBarShell = window.FindControl<Border>("SearchBarShell");
        var commandHintBar = window.FindControl<StackPanel>("CommandHintBar");
        var topHintProjectsButton = window.FindControl<Button>("TopHintProjectsButton");
        var topHintTerminalButton = window.FindControl<Button>("TopHintTerminalButton");
        var topHintMetaButton = window.FindControl<Button>("TopHintMetaButton");
        var resultList = window.FindControl<ListBox>("ResultList");
        var settingsButton = window.FindControl<Button>("SettingsOpenButton");

        Assert.NotNull(searchBox);
        Assert.NotNull(searchBarShell);
        Assert.NotNull(commandHintBar);
        Assert.NotNull(topHintProjectsButton);
        Assert.NotNull(topHintTerminalButton);
        Assert.NotNull(topHintMetaButton);
        Assert.NotNull(resultList);
        Assert.NotNull(settingsButton);
        Assert.Equal("Search input", AutomationProperties.GetName(searchBox));
        Assert.Equal("Switch to project search mode", AutomationProperties.GetName(topHintProjectsButton));
        Assert.Equal("Switch to terminal command mode", AutomationProperties.GetName(topHintTerminalButton));
        Assert.Equal("Switch to meta command mode", AutomationProperties.GetName(topHintMetaButton));
        Assert.Equal("Search results", AutomationProperties.GetName(resultList));
        Assert.Equal("Open settings panel", AutomationProperties.GetName(settingsButton));
    }

    [AvaloniaFact]
    public async Task MainWindow_OnboardingPending_ShowsOnboardingSurface()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Pending);
        using var viewModel = fixture.CreateViewModel();
        var window = new Launcher.App.MainWindow(viewModel);
        await window.InitializeAsync(CancellationToken.None);

        var overlay = window.FindControl<Border>("OnboardingOverlay");
        var hotkeyTextBox = window.FindControl<TextBox>("OnboardingHotkeyTextBox");
        var preferredToolComboBox = window.FindControl<ComboBox>("OnboardingPreferredToolComboBox");

        Assert.NotNull(overlay);
        Assert.NotNull(hotkeyTextBox);
        Assert.NotNull(preferredToolComboBox);
        Assert.True(overlay.IsVisible);
        Assert.Equal("Global hotkey", AutomationProperties.GetName(hotkeyTextBox));
        Assert.Equal("Preferred default tool", AutomationProperties.GetName(preferredToolComboBox));
    }

    [AvaloniaFact]
    public async Task MainWindow_SettingsOverlay_ExposesSettingsControls()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        var window = new Launcher.App.MainWindow(viewModel);
        await window.InitializeAsync(CancellationToken.None);

        viewModel.OpenSettings();

        var settingsOverlay = window.FindControl<Border>("SettingsOverlay");
        var settingsExitTextBox = window.FindControl<TextBox>("SettingsGeneralExitTextBox");
        var settingsThemeOverrideComboBox = window.FindControl<ComboBox>("SettingsThemeOverrideComboBox");
        var settingsSaveButton = window.FindControl<Button>("SettingsSaveButton");
        var settingsCancelButton = window.FindControl<Button>("SettingsCancelButton");
        var settingsCloseButton = window.FindControl<Button>("SettingsCloseButton");

        Assert.NotNull(settingsOverlay);
        Assert.NotNull(settingsExitTextBox);
        Assert.NotNull(settingsThemeOverrideComboBox);
        Assert.NotNull(settingsSaveButton);
        Assert.NotNull(settingsCancelButton);
        Assert.NotNull(settingsCloseButton);
        Assert.True(settingsOverlay.IsVisible);
        Assert.Equal("Theme override", AutomationProperties.GetName(settingsThemeOverrideComboBox));
        Assert.Equal("Save settings", AutomationProperties.GetName(settingsSaveButton));
        Assert.Equal("Cancel settings", AutomationProperties.GetName(settingsCancelButton));
        Assert.Equal("Close settings panel", AutomationProperties.GetName(settingsCloseButton));
        Assert.Contains("primaryButton", settingsSaveButton.Classes);
        Assert.Contains("secondaryButton", settingsCancelButton.Classes);
    }

    [AvaloniaFact]
    public async Task MainWindow_UsesStableLayoutShiftGuards()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Pending);
        using var viewModel = fixture.CreateViewModel();
        var window = new Launcher.App.MainWindow(viewModel);
        await window.InitializeAsync(CancellationToken.None);

        var resultList = window.FindControl<ListBox>("ResultList");
        var footerBar = window.FindControl<Grid>("FooterBar");
        var onboardingErrorSlot = window.FindControl<Border>("OnboardingErrorSlot");
        var onboardingActionBar = window.FindControl<StackPanel>("OnboardingActionBar");
        var finishButton = window.FindControl<Button>("OnboardingFinishButton");
        var skipButton = window.FindControl<Button>("OnboardingSkipButton");

        Assert.NotNull(resultList);
        Assert.NotNull(footerBar);
        Assert.NotNull(onboardingErrorSlot);
        Assert.NotNull(onboardingActionBar);
        Assert.NotNull(finishButton);
        Assert.NotNull(skipButton);
        Assert.Equal(Avalonia.Controls.Primitives.ScrollBarVisibility.Visible, ScrollViewer.GetVerticalScrollBarVisibility(resultList));
        Assert.Equal(34d, footerBar.MinHeight);
        Assert.True(footerBar.ColumnDefinitions[1].Width.IsAbsolute);
        Assert.Equal(340d, footerBar.ColumnDefinitions[1].Width.Value);
        Assert.Equal(18d, onboardingErrorSlot.MinHeight);
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Right, onboardingActionBar.HorizontalAlignment);
        Assert.Equal(110d, finishButton.Width);
        Assert.Equal(110d, skipButton.Width);
        Assert.Contains("primaryButton", finishButton.Classes);
        Assert.Contains("secondaryButton", skipButton.Classes);
    }

    [AvaloniaFact]
    public async Task MainWindow_SearchInput_TogglesAiMultilineDefaultsByMode()
    {
        var fixture = new ViewModelFixture_c(Launcher.Core.Models.OnboardingStates_c.Completed);
        using var viewModel = fixture.CreateViewModel();
        var window = new Launcher.App.MainWindow(viewModel);
        await window.InitializeAsync(CancellationToken.None);

        var searchBox = window.FindControl<TextBox>("SearchBox");
        var searchBarShell = window.FindControl<Border>("SearchBarShell");

        Assert.NotNull(searchBox);
        Assert.NotNull(searchBarShell);
        Assert.False(searchBox.AcceptsReturn);
        Assert.Equal(TextWrapping.NoWrap, searchBox.TextWrapping);
        Assert.Equal(1, searchBox.MinLines);
        Assert.Equal(1, searchBox.MaxLines);
        Assert.Equal(52d, searchBarShell.Height);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);

        Assert.True(searchBox.AcceptsReturn);
        Assert.Equal(TextWrapping.Wrap, searchBox.TextWrapping);
        Assert.Equal(3, searchBox.MinLines);
        Assert.Equal(3, searchBox.MaxLines);
        Assert.Equal(104d, searchBarShell.Height);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);

        Assert.False(searchBox.AcceptsReturn);
        Assert.Equal(TextWrapping.NoWrap, searchBox.TextWrapping);
        Assert.Equal(1, searchBox.MinLines);
        Assert.Equal(1, searchBox.MaxLines);
        Assert.Equal(52d, searchBarShell.Height);
    }

    [AvaloniaFact]
    public async Task MainWindow_AiResponseOverlay_VisibleOnlyDuringAiProcessing()
    {
        var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
        settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Completed;

        var fixture = new ViewModelFixture_c(settings, new SlowAiChatPort_c(delayMs: 150));
        using var viewModel = fixture.CreateViewModel();
        var window = new Launcher.App.MainWindow(viewModel);
        await window.InitializeAsync(CancellationToken.None);

        var overlay = window.FindControl<Border>("AiResponseBlockingOverlay");
        Assert.NotNull(overlay);
        Assert.False(overlay.IsVisible);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        viewModel.SearchText = "hello";

        var sendTask = viewModel.HandleEnterAsync();
        await Task.Delay(30);
        Assert.True(overlay.IsVisible);

        await sendTask;
        Assert.False(overlay.IsVisible);
    }

    [AvaloniaFact]
    public async Task MainWindow_AiEnterWhileBusy_ShowsBusyFeedback()
    {
        var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
        settings.Onboarding.State = Launcher.Core.Models.OnboardingStates_c.Completed;

        var fixture = new ViewModelFixture_c(settings, new SlowAiChatPort_c(delayMs: 180));
        using var viewModel = fixture.CreateViewModel();
        var window = new Launcher.App.MainWindow(viewModel);
        await window.InitializeAsync(CancellationToken.None);

        viewModel.SwitchToNextMode();
        await Task.Delay(40);
        viewModel.SearchText = "hello";

        var searchBarShell = window.FindControl<Border>("SearchBarShell");
        var aiResponsePanel = window.FindControl<Border>("AiResponsePanel");
        Assert.NotNull(searchBarShell);
        Assert.NotNull(aiResponsePanel);

        var sendTask = viewModel.HandleEnterAsync();
        await Task.Delay(25);
        await viewModel.HandleEnterAsync();

        var sawBusyClass = false;
        var sawShakeOffset = false;
        for (var attempt = 0; attempt < 12; attempt++)
        {
            await Task.Delay(20);
            sawBusyClass |= searchBarShell.Classes.Contains("busyReject");
            if (aiResponsePanel.RenderTransform is TranslateTransform translateTransform && Math.Abs(translateTransform.X) > 0.01d)
            {
                sawShakeOffset = true;
            }

            if (sawBusyClass && sawShakeOffset)
            {
                break;
            }
        }

        Assert.True(sawBusyClass);
        Assert.True(sawShakeOffset);

        await sendTask;
        await Task.Delay(180);
        Assert.DoesNotContain("busyReject", searchBarShell.Classes);
        if (aiResponsePanel.RenderTransform is TranslateTransform settledTransform)
        {
            Assert.Equal(0d, settledTransform.X);
        }
    }

    private sealed class ViewModelFixture_c
    {
        private readonly FakeSettingsStorePort_c _settingsStorePort;
        private readonly FakeProjectIndexPort_c _projectIndexPort;
        private readonly FakeToolDetectionPort_c _toolDetectionPort;
        private readonly FakeProjectLaunchPort_c _projectLaunchPort;

        public Launcher.Application.Runtime.LauncherApplication_o Application { get; }

        public ViewModelFixture_c(string onboardingState)
            : this(BuildSettingsWithState_c(onboardingState))
        {
        }

        public ViewModelFixture_c(Launcher.Core.Models.LauncherSettings_c settings)
            : this(settings, aiChatPort: null)
        {
        }

        public ViewModelFixture_c(Launcher.Core.Models.LauncherSettings_c settings, IAiChatPort_c? aiChatPort)
        {
            _settingsStorePort = new FakeSettingsStorePort_c(settings);
            _projectIndexPort = new FakeProjectIndexPort_c();
            _toolDetectionPort = new FakeToolDetectionPort_c(
            [
                new Launcher.Core.Models.ToolRecord_c
                {
                    ToolId = Launcher.Core.Models.ToolIds_c.VsCode,
                    DisplayName = "VS Code",
                    ExecutablePath = "code.exe",
                    IsAvailable = true
                },
                new Launcher.Core.Models.ToolRecord_c
                {
                    ToolId = Launcher.Core.Models.ToolIds_c.Rider,
                    DisplayName = "Rider",
                    ExecutablePath = "rider64.exe",
                    IsAvailable = true
                }
            ]);
            _projectLaunchPort = new FakeProjectLaunchPort_c();

            Application = new Launcher.Application.Runtime.LauncherApplication_o(
                _settingsStorePort,
                _projectIndexPort,
                _toolDetectionPort,
                _projectLaunchPort,
                aiChatPort: aiChatPort);
        }

        public MainWindowViewModel_o CreateViewModel()
        {
            return new MainWindowViewModel_o(Application);
        }

        public void EmitProjectsUpdated(IReadOnlyList<Launcher.Core.Models.ProjectRecord_c> projects)
        {
            _projectIndexPort.EmitProjectsUpdated(projects);
        }

        private static Launcher.Core.Models.LauncherSettings_c BuildSettingsWithState_c(string onboardingState)
        {
            var settings = Launcher.Core.Models.LauncherSettings_c.CreateDefault();
            settings.Onboarding.State = onboardingState;
            return settings;
        }
    }

    private sealed class FakeSettingsStorePort_c : ISettingsStorePort_c
    {
        private Launcher.Core.Models.LauncherSettings_c _settings;

        public FakeSettingsStorePort_c(Launcher.Core.Models.LauncherSettings_c settings)
        {
            _settings = settings;
        }

        public string SettingsPath => "C:\\launcher.settings.json";

        public Launcher.Core.Models.LauncherSettings_c LoadOrCreate()
        {
            return _settings;
        }

        public void Save(Launcher.Core.Models.LauncherSettings_c settings)
        {
            _settings = settings;
        }
    }

    private sealed class FakeProjectIndexPort_c : IProjectIndexPort_c
    {
        private List<Launcher.Core.Models.ProjectRecord_c> _projects =
        [
            new Launcher.Core.Models.ProjectRecord_c
            {
                ProjectName = "launcher",
                ProjectPath = "C:\\launcher",
                Landmarks = ["AGENTS.md"]
            },
            new Launcher.Core.Models.ProjectRecord_c
            {
                ProjectName = "library",
                ProjectPath = "C:\\library",
                Landmarks = ["README.md"]
            }
        ];

        public event EventHandler<IReadOnlyList<Launcher.Core.Models.ProjectRecord_c>>? ProjectsUpdated;

        public Launcher.Core.Models.ProjectIndexSnapshot_c LoadCachedSnapshot()
        {
            return new Launcher.Core.Models.ProjectIndexSnapshot_c
            {
                Projects = _projects.ToList()
            };
        }

        public Task InitializeAsync(Launcher.Core.Models.LauncherSettings_c settings, CancellationToken cancellationToken)
        {
            ProjectsUpdated?.Invoke(this, LoadCachedSnapshot().Projects);
            return Task.CompletedTask;
        }

        public Task RefreshAsync(CancellationToken cancellationToken)
        {
            ProjectsUpdated?.Invoke(this, LoadCachedSnapshot().Projects);
            return Task.CompletedTask;
        }

        public void EmitProjectsUpdated(IReadOnlyList<Launcher.Core.Models.ProjectRecord_c> projects)
        {
            _projects = projects.ToList();
            ProjectsUpdated?.Invoke(this, LoadCachedSnapshot().Projects);
        }

        public void MarkProjectOpened(string projectPath)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeToolDetectionPort_c : IToolDetectionPort_c
    {
        private readonly List<Launcher.Core.Models.ToolRecord_c> _tools;

        public FakeToolDetectionPort_c(List<Launcher.Core.Models.ToolRecord_c> tools)
        {
            _tools = tools;
        }

        public List<Launcher.Core.Models.ToolRecord_c> Detect(Launcher.Core.Models.LauncherSettings_c settings)
        {
            return _tools.ToList();
        }
    }

    private sealed class FakeProjectLaunchPort_c : IProjectLaunchPort_c
    {
        public bool Launch(Launcher.Core.Models.ProjectRecord_c project, Launcher.Core.Models.ToolRecord_c tool)
        {
            return true;
        }

        public bool LaunchFile(Launcher.Core.Models.ProjectRecord_c project, Launcher.Core.Models.ToolRecord_c tool, string filePath)
        {
            return true;
        }

        public bool OpenPathInShell(string path)
        {
            return true;
        }

        public bool LaunchWindowsTerminal(string projectPath)
        {
            return true;
        }
    }

    private sealed class SlowAiChatPort_c : IAiChatPort_c
    {
        private readonly int _delayMs;

        public SlowAiChatPort_c(int delayMs)
        {
            _delayMs = delayMs;
        }

        public async Task<Launcher.Application.Models.AiChatResult_c> ExecuteAsync(
            Launcher.Core.Models.AiChatSettings_c settings,
            string prompt,
            CancellationToken cancellationToken)
        {
            await Task.Delay(_delayMs, cancellationToken);
            return Launcher.Application.Models.AiChatResult_c.Success($"Echo: {prompt}");
        }
    }
}
