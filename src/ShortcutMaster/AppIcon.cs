using System.IO;

namespace ShortcutMaster;

/// <summary>アプリアイコン（exe 同梱・トレイ用）の読み込み。</summary>
internal static class AppIcon
{
    public static System.Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var assetPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(assetPath))
                return new System.Drawing.Icon(assetPath);

            var exePath = Environment.ProcessPath;
            if (exePath != null)
            {
                var extracted = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                if (extracted != null) return extracted;
            }
        }
        catch
        {
            // フォールバックへ
        }

        return System.Drawing.SystemIcons.Application;
    }
}
