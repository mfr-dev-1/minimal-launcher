using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Launcher.Infrastructure.Launch;

public static class WindowActivation_c
{
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    /// <summary>
    /// Finds a top-level window belonging to <paramref name="processName"/> whose title contains
    /// <paramref name="projectFolderName"/>, restores it, and brings it to the foreground.
    /// Returns true if a matching window was activated; false means the caller should launch fresh.
    /// </summary>
    public static bool TryActivateProjectWindow(string processName, string projectFolderName)
    {
        var processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0)
            return false;

        var pids = new HashSet<uint>(processes.Select(p => (uint)p.Id));
        IntPtr matchedHwnd = IntPtr.Zero;

        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (!pids.Contains(pid))
                return true; // continue

            int len = GetWindowTextLength(hWnd);
            if (len == 0)
                return true;

            var sb = new System.Text.StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();

            if (title.Contains(projectFolderName, StringComparison.OrdinalIgnoreCase))
            {
                matchedHwnd = hWnd;
                return false; // stop enumeration
            }

            return true;
        }, IntPtr.Zero);

        if (matchedHwnd == IntPtr.Zero)
            return false;

        ShowWindow(matchedHwnd, SW_RESTORE);
        SetForegroundWindow(matchedHwnd);
        return true;
    }
}
