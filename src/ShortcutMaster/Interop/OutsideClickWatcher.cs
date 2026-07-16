using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ShortcutMaster.Interop;

/// <summary>
/// 一覧パネル表示中のみ、範囲外クリックで閉じるための低レベルマウス監視。
/// NOACTIVATE のため LostFocus が使えないので WH_MOUSE_LL を使う。
/// </summary>
public sealed class OutsideClickWatcher : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;
    private const int WmXButtonDown = 0x020B;

    private readonly Dispatcher _dispatcher;
    private readonly Func<IEnumerable<Window?>> _windows;
    private readonly Action _onOutsideClick;

    private NativeMethods.LowLevelMouseProc? _proc;
    private IntPtr _hook;
    private int _armed;

    /// <summary>フック開始に失敗したときに1回通知したい場合。</summary>
    public event Action? ArmFailed;

    public OutsideClickWatcher(
        Dispatcher dispatcher,
        Func<IEnumerable<Window?>> windows,
        Action onOutsideClick)
    {
        _dispatcher = dispatcher;
        _windows = windows;
        _onOutsideClick = onOutsideClick;
    }

    public bool Arm()
    {
        if (Volatile.Read(ref _armed) == 1 && _hook != IntPtr.Zero)
            return true;

        Disarm();

        _proc = HookCallback;
        var hMod = Marshal.GetHINSTANCE(typeof(OutsideClickWatcher).Module);
        if (hMod == IntPtr.Zero)
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                hMod = NativeMethods.GetModuleHandle(process.MainModule?.ModuleName);
            }
            catch
            {
                hMod = NativeMethods.GetModuleHandle(null);
            }
        }

        _hook = NativeMethods.SetWindowsHookEx(WhMouseLl, _proc, hMod, 0);
        if (_hook == IntPtr.Zero)
        {
            _proc = null;
            ArmFailed?.Invoke();
            return false;
        }

        Volatile.Write(ref _armed, 1);
        return true;
    }

    public void Disarm()
    {
        Interlocked.Exchange(ref _armed, 0);

        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        _proc = null;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && Volatile.Read(ref _armed) == 1)
        {
            var msg = wParam.ToInt32();
            if (msg is WmLButtonDown or WmRButtonDown or WmMButtonDown or WmXButtonDown)
            {
                var info = Marshal.PtrToStructure<NativeMethods.MsLlHookStruct>(lParam);
                if (!IsPointOverOurUi(info.pt))
                {
                    // 閉じるクリックは下のアプリに届けない（メニュー閉じと同系統）
                    Interlocked.Exchange(ref _armed, 0);
                    _dispatcher.BeginInvoke(() =>
                    {
                        Disarm();
                        _onOutsideClick();
                    }, DispatcherPriority.Send);
                    return (IntPtr)1;
                }
            }
        }

        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private bool IsPointOverOurUi(NativeMethods.POINT pt)
    {
        try
        {
            foreach (var window in _windows())
            {
                if (window is not { IsVisible: true }) continue;

                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) continue;
                if (!NativeMethods.GetWindowRect(hwnd, out var rect)) continue;

                if (pt.X >= rect.Left && pt.X < rect.Right &&
                    pt.Y >= rect.Top && pt.Y < rect.Bottom)
                    return true;
            }
        }
        catch
        {
            // フック内は例外を外へ出さない
        }

        return false;
    }

    public void Dispose() => Disarm();
}
