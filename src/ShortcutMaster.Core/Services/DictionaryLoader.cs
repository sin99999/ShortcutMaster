using System.Text.Json;
using ShortcutMaster.Core.Models;
using ShortcutMaster.Core.Parsing;

namespace ShortcutMaster.Core.Services;

/// <summary>Data/*.json の読み込みと検証。</summary>
public static class DictionaryLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ShortcutDictionary LoadFile(string path)
    {
        var dict = JsonSerializer.Deserialize<ShortcutDictionary>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException($"辞書を読み込めません: {path}");
        return dict;
    }

    /// <summary>ディレクトリ内の全辞書を読み込む。壊れたファイルはスキップして errors に積む。</summary>
    public static List<ShortcutDictionary> LoadDirectory(string directory, List<string>? errors = null)
    {
        var result = new List<ShortcutDictionary>();
        if (!Directory.Exists(directory))
        {
            errors?.Add($"辞書フォルダーが見つかりません: {directory}");
            return result;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var dict = LoadFile(file);
                var validationErrors = Validate(dict);
                if (validationErrors.Count > 0)
                {
                    errors?.Add($"{Path.GetFileName(file)}: {string.Join("; ", validationErrors)}");
                    continue;
                }
                result.Add(dict);
            }
            catch (Exception ex)
            {
                errors?.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>辞書の整合性を検証し、問題の一覧を返す（空なら正常）。</summary>
    public static List<string> Validate(ShortcutDictionary dict)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dict.App)) errors.Add("app が空です。");
        if (string.IsNullOrWhiteSpace(dict.DisplayName)) errors.Add("displayName が空です。");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in dict.Entries)
        {
            var label = string.IsNullOrWhiteSpace(entry.Id) ? entry.Keys : entry.Id;
            if (string.IsNullOrWhiteSpace(entry.Id)) errors.Add($"id が空です: {label}");
            else if (!seen.Add(entry.Id)) errors.Add($"id が重複しています: {entry.Id}");

            if (string.IsNullOrWhiteSpace(entry.Keys)) errors.Add($"keys が空です: {label}");
            if (string.IsNullOrWhiteSpace(entry.Action)) errors.Add($"action が空です: {label}");
            if (string.IsNullOrWhiteSpace(entry.Category)) errors.Add($"category が空です: {label}");
            if (entry.Priority is < 0 or > 100) errors.Add($"priority は 0〜100 にしてください: {label}");

            if (entry.Send is { Length: > 0 })
            {
                try
                {
                    ChordParser.ParseSequence(entry.Send);
                }
                catch (Exception ex)
                {
                    errors.Add($"send を解釈できません ({label}): {ex.Message}");
                }
            }
        }

        return errors;
    }

    /// <summary>プロセス名に対応する辞書を返す。なければフォールバック辞書。</summary>
    public static ShortcutDictionary? ResolveForProcess(IEnumerable<ShortcutDictionary> dictionaries, string? processName)
    {
        ShortcutDictionary? fallback = null;
        foreach (var dict in dictionaries)
        {
            if (dict.IsFallback)
            {
                fallback ??= dict;
            }
            else if (processName != null &&
                     dict.ProcessNames.Any(n => n.Equals(processName, StringComparison.OrdinalIgnoreCase)))
            {
                return dict;
            }
        }

        return fallback;
    }
}
