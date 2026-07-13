using ShortcutMaster.Core.Services;

namespace ShortcutMaster.Core.Tests;

/// <summary>
/// 実際に同梱する Data/*.json を検証する。
/// 辞書に不正なキー名・重複 id などが混ざるとここで落ちる。
/// </summary>
public class DictionaryValidationTests
{
    private static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "Data");

    public static IEnumerable<object[]> DictionaryFiles()
        => Directory.GetFiles(DataDirectory, "*.json").Select(f => new object[] { f });

    [Fact]
    public void DataDirectory_ContainsThreeDictionaries()
    {
        Assert.True(Directory.Exists(DataDirectory), $"Data フォルダーがありません: {DataDirectory}");
        var files = Directory.GetFiles(DataDirectory, "*.json");
        Assert.True(files.Length >= 3, $"辞書は 3 ファイル以上必要です（現在 {files.Length}）");
    }

    [Theory]
    [MemberData(nameof(DictionaryFiles))]
    public void Dictionary_LoadsAndValidates(string path)
    {
        var dictionary = DictionaryLoader.LoadFile(path);
        var errors = DictionaryLoader.Validate(dictionary);

        Assert.True(errors.Count == 0,
            $"{Path.GetFileName(path)} の検証エラー:\n{string.Join("\n", errors)}");
        Assert.True(dictionary.Entries.Count >= 5,
            $"{Path.GetFileName(path)} のエントリが少なすぎます（{dictionary.Entries.Count} 件）");
    }

    [Fact]
    public void ResolveForProcess_MatchesExpectedDictionaries()
    {
        var dictionaries = DictionaryLoader.LoadDirectory(DataDirectory);

        Assert.Equal("excel", DictionaryLoader.ResolveForProcess(dictionaries, "EXCEL")!.App);
        Assert.Equal("cursor", DictionaryLoader.ResolveForProcess(dictionaries, "Cursor")!.App);
        Assert.Equal("windows", DictionaryLoader.ResolveForProcess(dictionaries, "notepad")!.App);
        Assert.Equal("windows", DictionaryLoader.ResolveForProcess(dictionaries, null)!.App);
    }

    [Fact]
    public void ExactlyOneFallbackDictionary()
    {
        var dictionaries = DictionaryLoader.LoadDirectory(DataDirectory);
        Assert.Equal(1, dictionaries.Count(d => d.IsFallback));
    }
}
