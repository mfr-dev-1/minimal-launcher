using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;

namespace Launcher.App.Services;

public sealed class GlobalHotkeyService_c : IDisposable
{
    private const int HotkeyId = 0xA11;
    private const uint WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private TopLevel? _hookedTopLevel;
    private Action? _callback;
    private Avalonia.Controls.Win32Properties.CustomWndProcHookCallback? _wndProcCallback;

    public bool Register(Window window, string hotkeyText, Action callback)
    {
        UnregisterCurrent_c();
        _callback = callback;

        var platformHandle = window.TryGetPlatformHandle();
        if (platformHandle is null || platformHandle.Handle == IntPtr.Zero)
        {
            return false;
        }

        if (!TryParseHotkey_c(hotkeyText, out var modifiers, out var keyCode))
        {
            return false;
        }

        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel is null)
        {
            return false;
        }

        _wndProcCallback = WndProc_c;
        Avalonia.Controls.Win32Properties.AddWndProcHookCallback(topLevel, _wndProcCallback);
        _hookedTopLevel = topLevel;

        if (!RegisterHotKey(platformHandle.Handle, HotkeyId, modifiers, keyCode))
        {
            UnregisterCurrent_c();
            return false;
        }

        return true;
    }

    public static bool IsValidHotkeyText(string hotkeyText)
    {
        return TryParseHotkey_c(hotkeyText, out _, out _);
    }

    public void Dispose()
    {
        UnregisterCurrent_c();
    }

    private IntPtr WndProc_c(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            _callback?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private static bool TryParseHotkey_c(string hotkeyText, out uint modifiers, out uint keyCode)
    {
        modifiers = 0;
        keyCode = 0;

        if (string.IsNullOrWhiteSpace(hotkeyText))
        {
            return false;
        }

        var parts = hotkeyText.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var keyPart = parts[^1];
        foreach (var part in parts[..^1])
        {
            if (part.Equals("alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModAlt;
                continue;
            }

            if (part.Equals("shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModShift;
                continue;
            }

            if (part.Equals("ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModControl;
                continue;
            }

            if (part.Equals("win", StringComparison.OrdinalIgnoreCase) || part.Equals("windows", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModWin;
            }
        }

        if (!Enum.TryParse<Key>(keyPart, true, out var parsedKey))
        {
            return false;
        }

        keyCode = (uint)Avalonia.Win32.Input.KeyInterop.VirtualKeyFromKey(parsedKey);
        return keyCode != 0;
    }

    private void UnregisterCurrent_c()
    {
        if (_hookedTopLevel is null)
        {
            return;
        }

        var handle = _hookedTopLevel.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle != IntPtr.Zero)
        {
            UnregisterHotKey(handle, HotkeyId);
        }

        if (_wndProcCallback is not null)
        {
            Avalonia.Controls.Win32Properties.RemoveWndProcHookCallback(_hookedTopLevel, _wndProcCallback);
        }

        _hookedTopLevel = null;
        _wndProcCallback = null;
    }
}
