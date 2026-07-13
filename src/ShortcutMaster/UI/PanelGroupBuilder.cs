using ShortcutMaster.Core.Models;
using ShortcutMaster.Core.Services;

namespace ShortcutMaster.UI;

/// <summary>
/// 辞書からパネル表示用のグループ列を作る。
/// 設計: 先頭に「おすすめ」（スコア上位）、続いてカテゴリ（ファイル内初出順）、
/// アプリ専用辞書のときは末尾に「Windows 共通」を少しだけ足す。
/// </summary>
public static class PanelGroupBuilder
{
    private const int RecommendedCount = 8;
    private const int CommonCount = 12;

    public static List<GroupVm> Build(
        ShortcutDictionary dictionary,
        ShortcutDictionary? windowsCommon,
        Func<string, int> usageLookup)
    {
        var groups = new List<GroupVm>();

        var sorted = EntryScorer.SortByScore(dictionary.Entries, usageLookup);
        if (sorted.Count > 0)
            groups.Add(new GroupVm { Name = "おすすめ", Rows = ToRows(sorted.Take(RecommendedCount)) });

        var categoryOrder = new List<string>();
        var byCategory = new Dictionary<string, List<ShortcutEntry>>();
        foreach (var entry in dictionary.Entries)
        {
            if (!byCategory.TryGetValue(entry.Category, out var list))
            {
                list = new List<ShortcutEntry>();
                byCategory[entry.Category] = list;
                categoryOrder.Add(entry.Category);
            }
            list.Add(entry);
        }

        foreach (var category in categoryOrder)
            groups.Add(new GroupVm { Name = category, Rows = ToRows(EntryScorer.SortByScore(byCategory[category], usageLookup)) });

        if (windowsCommon != null && windowsCommon.Entries.Count > 0)
        {
            groups.Add(new GroupVm
            {
                Name = $"{windowsCommon.DisplayName} 共通",
                Rows = ToRows(EntryScorer.SortByScore(windowsCommon.Entries, usageLookup).Take(CommonCount)),
            });
        }

        return groups;
    }

    private static List<RowVm> ToRows(IEnumerable<ShortcutEntry> entries)
        => entries.Select(e => new RowVm { Entry = e }).ToList();
}
