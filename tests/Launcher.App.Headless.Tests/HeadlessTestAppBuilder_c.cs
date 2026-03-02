using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Launcher.App.Headless.Tests.HeadlessTestAppBuilder_c))]

namespace Launcher.App.Headless.Tests;

public static class HeadlessTestAppBuilder_c
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<HeadlessTestApp_c>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}

public sealed class HeadlessTestApp_c : Avalonia.Application
{
}
