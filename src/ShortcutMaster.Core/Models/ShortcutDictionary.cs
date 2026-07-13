namespace ShortcutMaster.Core.Models;

/// <summary>1 アプリ分のショートカット辞書（Data/*.json 1 ファイルに対応）。</summary>
public sealed class ShortcutDictionary
{
    public string App { get; set; } = "";

    public string DisplayName { get; set; } = "";

    /// <summary>対象プロセス名（拡張子なし）。空 = フォールバック辞書。</summary>
    public string[] ProcessNames { get; set; } = Array.Empty<string>();

    public List<ShortcutEntry> Entries { get; set; } = new();

    public bool IsFallback => ProcessNames.Length == 0;
}
