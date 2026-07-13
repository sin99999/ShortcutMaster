using ShortcutMaster.Core.Models;
using ShortcutMaster.Core.Services;

namespace ShortcutMaster.Core.Tests;

public class PanelCatalogBuilderTests
{
    private static ShortcutEntry Entry(string id, string category, int priority)
        => new()
        {
            Id = id,
            Keys = "Ctrl+A",
            Action = "テスト",
            Category = category,
            Priority = priority,
            Send = ["Ctrl+A"],
        };

    [Fact]
    public void Build_DeduplicatesRecommendedFromCategories()
    {
        var dictionary = new ShortcutDictionary
        {
            App = "test",
            DisplayName = "Test",
            Entries = Enumerable.Range(1, 10)
                .Select(i => Entry($"e{i}", "基本", 110 - i))
                .ToList(),
        };

        var groups = PanelCatalogBuilder.Build(dictionary, windowsCommon: null, _ => 0);

        var recommended = groups[0];
        Assert.Equal("おすすめ", recommended.Name);
        Assert.Equal(8, recommended.Entries.Count);

        var basic = groups.First(g => g.Name == "基本");
        Assert.Equal(2, basic.Entries.Count);
        Assert.DoesNotContain(basic.Entries, e => recommended.Entries.Any(r => r.Id == e.Id));
    }

    [Fact]
    public void EntryBelongsToContext_MatchesAppAndWindowsCommon()
    {
        var windows = new ShortcutDictionary
        {
            App = "windows",
            DisplayName = "Windows",
            ProcessNames = [],
            Entries = [Entry("win.copy", "基本", 90)],
        };
        var excel = new ShortcutDictionary
        {
            App = "excel",
            DisplayName = "Excel",
            ProcessNames = ["EXCEL"],
            Entries = [Entry("xls.save", "基本", 90)],
        };

        Assert.True(DictionaryLoader.EntryBelongsToContext(excel.Entries[0], excel, windows));
        Assert.True(DictionaryLoader.EntryBelongsToContext(windows.Entries[0], excel, windows));
        Assert.False(DictionaryLoader.EntryBelongsToContext(excel.Entries[0], windows, windows));
    }
}
