using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ShortcutMaster.Interop;

/// <summary>
/// UIPI 対策: 管理者権限（昇格）プロセスへの SendInput は黙って失敗するため、
/// 送信前に権限差を検出して日本語で案内する。
/// </summary>
internal static class ElevationChecker
{
    public static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>true=昇格 / false=非昇格 / null=判定不能（アクセス拒否は昇格の可能性が高い）。</summary>
    public static bool? IsProcessElevated(uint pid)
    {
        var process = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process == IntPtr.Zero) return null;

        try
        {
            if (!NativeMethods.OpenProcessToken(process, NativeMethods.TOKEN_QUERY, out var token))
                return null;

            try
            {
                var buffer = Marshal.AllocHGlobal(sizeof(int));
                try
                {
                    if (!NativeMethods.GetTokenInformation(token, NativeMethods.TOKEN_ELEVATION_CLASS, buffer, sizeof(int), out _))
                        return null;
                    return Marshal.ReadInt32(buffer) != 0;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                NativeMethods.CloseHandle(token);
            }
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }
}
