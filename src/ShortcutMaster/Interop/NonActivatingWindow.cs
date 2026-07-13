using System.Windows;
using System.Windows.Interop;

namespace ShortcutMaster.Interop;

/// <summary>
/// ウィンドウを「クリックしてもキーボードフォーカスを奪わない」状態にする。
/// これが ShortcutMaster の生命線（Ctrl+C 等を前面アプリに正しく届けるため）。
/// </summary>
internal static class NonActivatingWindow
{
    /// <summary>OnSourceInitialized 以降に呼ぶこと。</summary>
    public static void Apply(Window window, bool clickThrough = false)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;

        var exStyle = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        exStyle |= NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW;
        if (clickThrough) exStyle |= NativeMethods.WS_EX_TRANSPARENT;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));

        HwndSource.FromHwnd(handle)?.AddHook(WndProc);
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_MOUSEACTIVATE)
        {
            handled = true;
            return new IntPtr(NativeMethods.MA_NOACTIVATE);
        }

        return IntPtr.Zero;
    }
}
