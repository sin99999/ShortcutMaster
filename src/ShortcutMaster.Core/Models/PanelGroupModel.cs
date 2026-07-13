namespace ShortcutMaster.Core.Models;

/// <summary>パネル表示用のグループ（見出し + エントリ列）。</summary>
public sealed class PanelGroupModel
{
    public required string Name { get; init; }
    public required IReadOnlyList<ShortcutEntry> Entries { get; init; }
}
