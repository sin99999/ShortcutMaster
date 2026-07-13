namespace ShortcutMaster.Core.Models;

/// <summary>辞書 JSON の 1 エントリ。</summary>
public sealed class ShortcutEntry
{
    public string Id { get; set; } = "";

    /// <summary>表示用のキー文字列（例: "Ctrl+Shift+V"、順押しは "Alt, H, B"）。</summary>
    public string Keys { get; set; } = "";

    /// <summary>簡潔な日本語説明。</summary>
    public string Action { get; set; } = "";

    /// <summary>補足（任意）。</summary>
    public string? Note { get; set; }

    public string Category { get; set; } = "";

    /// <summary>0〜100。大きいほど上位に表示される。</summary>
    public int Priority { get; set; }

    /// <summary>送信するキー手順。null または空 = 表示のみ。</summary>
    public string[]? Send { get; set; }

    /// <summary>
    /// 既に起動中なら前面へ出すプロセス名（拡張子なし）。
    /// 見つからなければ send で起動を試みる。
    /// </summary>
    public string? FocusProcess { get; set; }

    public bool IsSendable => Send is { Length: > 0 };
}
