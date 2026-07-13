namespace ShortcutMaster.Core.Parsing;

/// <summary>
/// 送信手順の 1 ステップ。
/// 「Ctrl+Shift+V」のような同時押し、または「Alt」単体タップを表す。
/// </summary>
public sealed record ChordStep(IReadOnlyList<KeyDef> Modifiers, KeyDef Key, bool IsModifierTap);

/// <summary>辞書 JSON の send トークンを解釈する。</summary>
public static class ChordParser
{
    public static ChordStep ParseStep(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new FormatException("空のショートカット指定です。");

        var parts = token.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            throw new FormatException($"ショートカットを解釈できません: {token}");

        var modifiers = new List<KeyDef>(parts.Length - 1);
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (!VirtualKeyMap.IsModifier(parts[i]))
                throw new FormatException($"修飾キーではありません: {parts[i]} ({token})");
            VirtualKeyMap.TryGet(parts[i], out var mod);
            modifiers.Add(mod);
        }

        var keyName = parts[^1];
        if (!VirtualKeyMap.TryGet(keyName, out var key))
            throw new FormatException($"不明なキー名です: {keyName} ({token})");

        var isTap = parts.Length == 1 && VirtualKeyMap.IsModifier(keyName);
        return new ChordStep(modifiers, key, isTap);
    }

    public static IReadOnlyList<ChordStep> ParseSequence(IEnumerable<string> tokens)
        => tokens.Select(ParseStep).ToArray();
}
