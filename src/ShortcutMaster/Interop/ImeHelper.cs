using System.Runtime.InteropServices;

namespace ShortcutMaster.Interop;

/// <summary>日本語 IME の変換状態を簡易判定する（imm32）。</summary>
internal static class ImeHelper
{
    private const int GcsCompStr = 0x0008;

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetContext(IntPtr hWnd);

    [DllImport("imm32.dll")]
    private static extern bool ImmGetOpenStatus(IntPtr hIMC);

    [DllImport("imm32.dll")]
    private static extern int ImmGetCompositionString(IntPtr hIMC, int dwIndex, byte[]? lpBuf, int dwBufLen);

    [DllImport("imm32.dll")]
    private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

    /// <summary>IME で文字を変換入力中の場合 true。</summary>
    public static bool IsImeComposing(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;

        var context = ImmGetContext(hwnd);
        if (context == IntPtr.Zero) return false;

        try
        {
            if (!ImmGetOpenStatus(context)) return false;
            return ImmGetCompositionString(context, GcsCompStr, null, 0) > 0;
        }
        finally
        {
            ImmReleaseContext(hwnd, context);
        }
    }
}
