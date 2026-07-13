using ShortcutMaster.Core.Models;
using ShortcutMaster.Core.Services;

namespace ShortcutMaster.Core.Tests;

public class ScorerAndUsageTests
{
    private static ShortcutEntry Entry(string id, int priority)
        => new() { Id = id, Keys = "Ctrl+A", Action = "テスト", Category = "基本", Priority = priority };

    [Fact]
    public void Score_UsageBoost_IsCapped()
    {
        var entry = Entry("a", 50);

        Assert.Equal(50, EntryScorer.Score(entry, 0));
        Assert.Equal(52, EntryScorer.Score(entry, 1));
        Assert.Equal(130, EntryScorer.Score(entry, 40));
        Assert.Equal(130, EntryScorer.Score(entry, 9999));
    }

    [Fact]
    public void SortByScore_UsageOvertakesPriority()
    {
        var high = Entry("high", 90);
        var low = Entry("low", 70);
        var usage = new Dictionary<string, int> { ["low"] = 15 };

        var sorted = EntryScorer.SortByScore(new[] { high, low }, id => usage.GetValueOrDefault(id));

        Assert.Equal("low", sorted[0].Id); // 70 + 30 = 100 > 90
    }

    [Fact]
    public void UsageStore_RoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sm_usage_{Guid.NewGuid():N}.json");
        try
        {
            var store = new UsageStore(path);
            Assert.Equal(0, store.GetCount("win.copy"));

            store.Increment("win.copy");
            store.Increment("win.copy");
            store.Increment("win.paste");
            store.FlushPending();

            var reloaded = new UsageStore(path);
            Assert.Equal(2, reloaded.GetCount("win.copy"));
            Assert.Equal(1, reloaded.GetCount("win.paste"));
            Assert.Equal(0, reloaded.GetCount("unknown"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void UsageStore_BrokenFile_StartsEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sm_usage_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{ broken json !!");
            var store = new UsageStore(path);
            Assert.Equal(0, store.GetCount("anything"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
