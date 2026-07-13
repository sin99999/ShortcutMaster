using System.Diagnostics;

namespace ShortcutMaster.Interop;

public sealed record ForegroundInfo(IntPtr Hwnd, string ProcessName);

/// <summary>
/// 前面ウィンドウの切り替わりを監視する。
/// 自プロセスのウィンドウは無視する（NOACTIVATE のため通常は発生しない）。
/// </summary>
public sealed class ForegroundTracker : IDisposable
{
    // GC に回収されないようデリゲートをフィールドで保持する（必須）
    private NativeMethods.WinEventDelegate? _callback;
    private IntPtr _hook;
    private readonly uint _ownPid = (uint)Environment.ProcessId;

    /// <summary>直近の（自分以外の）前面ウィンドウ。</summary>
    public ForegroundInfo? Current { get; private set; }

    /// <summary>フック元スレッド（UI スレッド）で発火する。</summary>
    public event Action<ForegroundInfo>? Changed;

    public void Start()
    {
        Capture(NativeMethods.GetForegroundWindow());
        _callback = OnWinEvent;
        _hook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _callback, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
    }

    private void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
    {
        if (hwnd != IntPtr.Zero) Capture(hwnd);
    }

    private void Capture(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;

        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0 || pid == _ownPid) return;

        string processName;
        try
        {
            using var process = Process.GetProcessById((int)pid);
            processName = process.ProcessName;
        }
        catch
        {
            return;
        }

        var info = new ForegroundInfo(hwnd, processName);
        Current = info;
        Changed?.Invoke(info);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
        _callback = null;
    }
}
