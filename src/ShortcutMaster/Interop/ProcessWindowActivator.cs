using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ShortcutMaster.Interop;

public enum ActivateOutcome
{
    NotRunning,
    Activated,
    FailedElevated,
    FailedForeground,
}

/// <summary>既に起動中のプロセスのメインウィンドウを前面へ出す。</summary>
public static class ProcessWindowActivator
{
    private const int SwRestore = 9;

    public static ActivateOutcome TryActivate(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return ActivateOutcome.NotRunning;

        var hwnd = FindBestWindow(processName);
        if (hwnd == IntPtr.Zero)
            return ActivateOutcome.NotRunning;

        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        if (!ElevationChecker.IsCurrentProcessElevated() && ElevationChecker.IsProcessElevated(pid) == true)
            return ActivateOutcome.FailedElevated;

        return BringToForeground(hwnd) ? ActivateOutcome.Activated : ActivateOutcome.FailedForeground;
    }

    private static IntPtr FindBestWindow(string processName)
    {
        IntPtr best = IntPtr.Zero;
        var bestScore = 0;

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindow(hwnd) || !NativeMethods.IsWindowVisible(hwnd))
                return true;

            if (NativeMethods.GetWindow(hwnd, NativeMethods.GwOwner) != IntPtr.Zero)
                return true;

            var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExstyle);
            if ((exStyle.ToInt64() & NativeMethods.WS_EX_TOOLWINDOW) != 0)
                return true;

            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            if (!TryGetProcessName(pid, out var name) ||
                !name.Equals(processName, StringComparison.OrdinalIgnoreCase))
                return true;

            var score = 1;
            if (GetWindowTitle(hwnd).Length > 0)
                score += 10;

            if (NativeMethods.GetWindowRect(hwnd, out var rect))
            {
                var area = Math.Max(0, rect.Right - rect.Left) * Math.Max(0, rect.Bottom - rect.Top);
                score += Math.Min(area / 10_000, 50);
            }

            if (score <= bestScore)
                return true;

            bestScore = score;
            best = hwnd;
            return true;
        }, IntPtr.Zero);

        return best;
    }

    private static bool BringToForeground(IntPtr hwnd)
    {
        if (NativeMethods.IsIconic(hwnd))
            NativeMethods.ShowWindow(hwnd, SwRestore);

        var fg = NativeMethods.GetForegroundWindow();
        var fgThread = NativeMethods.GetWindowThreadProcessId(fg, out _);
        var targetThread = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        var currentThread = NativeMethods.GetCurrentThreadId();

        var attachedFg = false;
        var attachedTarget = false;
        try
        {
            if (fgThread != currentThread)
                attachedFg = NativeMethods.AttachThreadInput(currentThread, fgThread, true);
            if (targetThread != currentThread)
                attachedTarget = NativeMethods.AttachThreadInput(currentThread, targetThread, true);

            NativeMethods.BringWindowToTop(hwnd);
            NativeMethods.SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attachedTarget)
                NativeMethods.AttachThreadInput(currentThread, targetThread, false);
            if (attachedFg)
                NativeMethods.AttachThreadInput(currentThread, fgThread, false);
        }

        return NativeMethods.GetForegroundWindow() == hwnd;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var sb = new StringBuilder(512);
        return NativeMethods.GetWindowText(hwnd, sb, sb.Capacity) > 0 ? sb.ToString() : "";
    }

    private static bool TryGetProcessName(uint pid, out string name)
    {
        name = "";
        try
        {
            using var process = Process.GetProcessById((int)pid);
            name = process.ProcessName;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
