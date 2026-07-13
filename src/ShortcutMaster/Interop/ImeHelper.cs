using System.Runtime.InteropServices;

namespace ShortcutMaster.Interop;

/// <summary>日本語 IME の変換状態を簡易判定する（imm32）。</summary>
internal static class ImeHelper
{
    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetContext(IntPtr hWnd);

    [DllImport("imm32.dll")]
    private static extern bool ImmGetOpenStatus(IntPtr hIMC);

    [DllImport("imm32.dll")]
    private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

    /// <summary>IME が開いている（変換候補入力中の可能性が高い）場合 true。</summary>
    public static bool IsImeOpen(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;

        var context = ImmGetContext(hwnd);
        if (context == IntPtr.Zero) return false;

        try
        {
            return ImmGetOpenStatus(context);
        }
        finally
        {
            ImmReleaseContext(hwnd, context);
        }
    }
}
