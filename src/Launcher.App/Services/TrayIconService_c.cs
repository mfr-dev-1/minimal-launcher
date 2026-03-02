using System.Drawing;
using System.Drawing.Imaging;

namespace Launcher.App.Services;

public sealed class TrayIconService_c : IDisposable
{
    private Avalonia.Controls.TrayIcon? _trayIcon;

    public void Initialize(Action onOpen, Action onExit)
    {
        var menu = new Avalonia.Controls.NativeMenu();
        var openItem = new Avalonia.Controls.NativeMenuItem("Open Launcher");
        openItem.Click += (_, _) => onOpen();
        var exitItem = new Avalonia.Controls.NativeMenuItem("Exit");
        exitItem.Click += (_, _) => onExit();
        menu.Items.Add(openItem);
        menu.Items.Add(new Avalonia.Controls.NativeMenuItemSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new Avalonia.Controls.TrayIcon
        {
            ToolTipText = "Minimal Launcher",
            Menu = menu,
            Icon = BuildDefaultIcon_c(),
            IsVisible = true
        };
        _trayIcon.Clicked += (_, _) => onOpen();
    }

    public void ShowMessage(string title, string body)
    {
        // Avalonia tray API does not provide native balloon notifications consistently.
        // Keep intent explicit by writing to stderr for debug sessions.
        Console.Error.WriteLine($"[{title}] {body}");
    }

    public void Dispose()
    {
        if (_trayIcon is null)
        {
            return;
        }

        _trayIcon.IsVisible = false;
        _trayIcon.Dispose();
        _trayIcon = null;
    }

    private static Avalonia.Controls.WindowIcon BuildDefaultIcon_c()
    {
        using var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(21, 39, 52));
        graphics.DrawRectangle(Pens.DeepSkyBlue, 0, 0, 15, 15);
        graphics.FillRectangle(Brushes.DeepSkyBlue, 4, 4, 8, 8);

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        return new Avalonia.Controls.WindowIcon(stream);
    }
}
