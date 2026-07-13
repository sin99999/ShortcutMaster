using System.Windows;
using ShortcutMaster.Core.Models;

namespace ShortcutMaster.UI;

/// <summary>パネルの 1 行分。</summary>
public sealed class RowVm
{
    public required ShortcutEntry Entry { get; init; }

    public string KeysText => Entry.Keys;
    public string ActionText => Entry.Action;
    public string? NoteText => string.IsNullOrWhiteSpace(Entry.Note) ? null : Entry.Note;
    public bool IsSendable => Entry.IsSendable;

    public Visibility NoteVisibility => NoteText is null ? Visibility.Collapsed : Visibility.Visible;
    public Visibility DisplayOnlyVisibility => IsSendable ? Visibility.Collapsed : Visibility.Visible;
}

/// <summary>パネルのグループ（見出し + 行）。</summary>
public sealed class GroupVm
{
    public required string Name { get; init; }
    public required List<RowVm> Rows { get; init; }
}
