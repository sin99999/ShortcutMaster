using ShortcutMaster.Core.Models;

namespace ShortcutMaster.Core.Services;

/// <summary>
/// 辞書からパネル表示用のグループ列を作る。
/// 先頭に「おすすめ」、続いてカテゴリ（おすすめと重複する行は除く）、
/// アプリ専用辞書のときは末尾に「Windows 共通」を足す。
/// </summary>
public static class PanelCatalogBuilder
{
    private const int RecommendedCount = 8;
    private const int CommonCount = 12;

    public static List<PanelGroupModel> Build(
        ShortcutDictionary dictionary,
        ShortcutDictionary? windowsCommon,
        Func<string, int> usageLookup)
    {
        var groups = new List<PanelGroupModel>();
        var sorted = EntryScorer.SortByScore(dictionary.Entries, usageLookup);
        var recommended = sorted.Take(RecommendedCount).ToList();
        var recommendedIds = new HashSet<string>(
            recommended.Select(e => e.Id),
            StringComparer.OrdinalIgnoreCase);

        if (recommended.Count > 0)
        {
            groups.Add(new PanelGroupModel
            {
                Name = "おすすめ",
                Entries = recommended,
            });
        }

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
        {
            var entries = EntryScorer.SortByScore(
                    byCategory[category].Where(e => !recommendedIds.Contains(e.Id)),
                    usageLookup)
                .ToList();
            if (entries.Count == 0) continue;

            groups.Add(new PanelGroupModel
            {
                Name = category,
                Entries = entries,
            });
        }

        if (windowsCommon != null && windowsCommon.Entries.Count > 0)
        {
            groups.Add(new PanelGroupModel
            {
                Name = $"{windowsCommon.DisplayName} 共通",
                Entries = EntryScorer.SortByScore(windowsCommon.Entries, usageLookup)
                    .Take(CommonCount)
                    .ToList(),
            });
        }

        return groups;
    }
}
