using Avalonia;

namespace Launcher.App;

internal static class Program
{
    private const string SingleInstanceMutexName = "Launcher.App.SingleInstance";

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new System.Threading.Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool acquired);
        if (!acquired)
        {
            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
