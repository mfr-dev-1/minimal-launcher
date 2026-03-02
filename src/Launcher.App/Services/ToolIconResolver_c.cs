using System.Drawing;
using System.Drawing.Imaging;
using Launcher.Core.Models;

namespace Launcher.App.Services;

public sealed class ToolIconResolver_c
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Avalonia.Media.IImage> _cache = new(StringComparer.OrdinalIgnoreCase);

    public Avalonia.Media.IImage Resolve(Launcher.Core.Models.ToolRecord_c tool)
    {
        var cacheKey = BuildCacheKey_c(tool.ToolId, tool.ExecutablePath);
        lock (_gate)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
        }

        var fromExecutable = TryExtractExecutableIcon_c(tool.ExecutablePath);
        var resolved = fromExecutable ?? BuildFallbackGlyphIcon_c(tool.ToolId, tool.DisplayName);

        lock (_gate)
        {
            _cache[cacheKey] = resolved;
        }

        return resolved;
    }

    public Avalonia.Media.IImage ResolveNeutral()
    {
        const string cacheKey = "neutral";
        lock (_gate)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
        }

        var generated = BuildNeutralIcon_c();
        lock (_gate)
        {
            _cache[cacheKey] = generated;
        }

        return generated;
    }

    private static string BuildCacheKey_c(string toolId, string executablePath)
    {
        return $"{toolId.Trim()}|{executablePath.Trim()}";
    }

    private static Avalonia.Media.IImage? TryExtractExecutableIcon_c(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return null;
        }

        try
        {
            using var icon = Icon.ExtractAssociatedIcon(executablePath);
            if (icon is null)
            {
                return null;
            }

            using var bitmap = icon.ToBitmap();
            return ConvertBitmapToAvaloniaImage_c(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private static Avalonia.Media.IImage BuildFallbackGlyphIcon_c(string toolId, string displayName)
    {
        var background = ResolveBackgroundColor_c(toolId);
        var glyph = ResolveGlyph_c(toolId, displayName);

        using var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(background);
        graphics.DrawRectangle(Pens.Black, 0, 0, 15, 15);
        using var font = new Font("Consolas", glyph.Length > 1 ? 6.7f : 8.6f, FontStyle.Bold, GraphicsUnit.Pixel);
        var textSize = graphics.MeasureString(glyph, font);
        var textX = Math.Max(0f, (16f - textSize.Width) / 2f);
        var textY = Math.Max(0f, (16f - textSize.Height) / 2f);
        graphics.DrawString(glyph, font, Brushes.White, textX, textY);
        return ConvertBitmapToAvaloniaImage_c(bitmap);
    }

    private static Avalonia.Media.IImage BuildNeutralIcon_c()
    {
        using var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.SlateGray);
        graphics.DrawRectangle(Pens.Black, 0, 0, 15, 15);
        return ConvertBitmapToAvaloniaImage_c(bitmap);
    }

    private static Avalonia.Media.IImage ConvertBitmapToAvaloniaImage_c(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        return new Avalonia.Media.Imaging.Bitmap(stream);
    }

    private static Color ResolveBackgroundColor_c(string toolId)
    {
        return toolId.ToLowerInvariant() switch
        {
            ToolIds_c.VisualStudio => Color.MediumPurple,
            ToolIds_c.VsCode => Color.DeepSkyBlue,
            ToolIds_c.Rider => Color.HotPink,
            ToolIds_c.IntelliJ => Color.Orange,
            ToolIds_c.PyCharm => Color.LimeGreen,
            ToolIds_c.WebStorm => Color.Cyan,
            ToolIds_c.CLion => Color.DodgerBlue,
            ToolIds_c.GoLand => Color.Teal,
            ToolIds_c.PhpStorm => Color.SteelBlue,
            ToolIds_c.RustRover => Color.OrangeRed,
            ToolIds_c.AndroidStudio => Color.ForestGreen,
            ToolIds_c.Eclipse => Color.SlateBlue,
            ToolIds_c.Cursor => Color.Gold,
            ToolIds_c.Windsurf => Color.LightSeaGreen,
            ToolIds_c.SublimeText => Color.DarkOrange,
            ToolIds_c.NotepadPlusPlus => Color.SeaGreen,
            ToolIds_c.Vim => Color.OliveDrab,
            ToolIds_c.NeoVim => Color.MediumSeaGreen,
            _ => Color.SteelBlue
        };
    }

    private static string ResolveGlyph_c(string toolId, string displayName)
    {
        var fromToolId = toolId.ToLowerInvariant() switch
        {
            ToolIds_c.VisualStudio => "VS",
            ToolIds_c.VsCode => "C",
            ToolIds_c.IntelliJ => "IJ",
            ToolIds_c.PyCharm => "PY",
            ToolIds_c.WebStorm => "WS",
            ToolIds_c.Rider => "R",
            ToolIds_c.CLion => "CL",
            ToolIds_c.GoLand => "GO",
            ToolIds_c.PhpStorm => "PH",
            ToolIds_c.RustRover => "RR",
            ToolIds_c.AndroidStudio => "AS",
            ToolIds_c.Eclipse => "E",
            ToolIds_c.Cursor => "CU",
            ToolIds_c.Windsurf => "WF",
            ToolIds_c.SublimeText => "S",
            ToolIds_c.NotepadPlusPlus => "N+",
            ToolIds_c.Vim => "V",
            ToolIds_c.NeoVim => "NV",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(fromToolId))
        {
            return fromToolId;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            var letters = new string(displayName.Where(char.IsLetterOrDigit).Take(2).ToArray()).ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(letters))
            {
                return letters;
            }
        }

        return "?";
    }
}
