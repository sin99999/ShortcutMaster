using ShortcutMaster.Core.Models;

namespace ShortcutMaster.Core.Services;

/// <summary>優先度と使用回数から表示順を決める。</summary>
public static class EntryScorer
{
    /// <summary>使用回数の加点は 40 回で頭打ち（priority を無限に追い越さないため）。</summary>
    public static int Score(ShortcutEntry entry, int usageCount)
        => entry.Priority + Math.Min(Math.Max(usageCount, 0), 40) * 2;

    public static List<ShortcutEntry> SortByScore(IEnumerable<ShortcutEntry> entries, Func<string, int> usageLookup)
        => entries
            .OrderByDescending(e => Score(e, usageLookup(e.Id)))
            .ThenBy(e => e.Keys, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
