using Avalonia.Input;
using Launcher.App.Services;
using Launcher.Core.Models;

namespace Launcher.App.Headless.Tests;

public sealed class InAppHotkeyRouterTests
{
    [Fact]
    public void IsSwitchMode_RecognizesShiftTabAndOemBackTab()
    {
        var settings = GeneralHotkeySettings_c.CreateDefault();
        settings.SwitchMode = "Shift+Tab";

        var router = new InAppHotkeyRouter_c(settings);

        Assert.True(router.IsSwitchMode(Key.Tab, KeyModifiers.Shift));
        Assert.True(router.IsSwitchMode(Key.OemBackTab, KeyModifiers.None));
        Assert.False(router.IsSwitchMode(Key.Tab, KeyModifiers.None));
    }

    [Fact]
    public void IsSwitchMode_UsesConfiguredOverrideInsteadOfDefaultShiftTab()
    {
        var settings = GeneralHotkeySettings_c.CreateDefault();
        settings.SwitchMode = "Ctrl+K";

        var router = new InAppHotkeyRouter_c(settings);

        Assert.True(router.IsSwitchMode(Key.K, KeyModifiers.Control));
        Assert.False(router.IsSwitchMode(Key.Tab, KeyModifiers.Shift));
        Assert.False(router.IsSwitchMode(Key.OemBackTab, KeyModifiers.None));
    }
}
