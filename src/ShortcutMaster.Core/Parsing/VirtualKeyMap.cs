namespace ShortcutMaster.Core.Parsing;

/// <summary>仮想キーコードと拡張キーフラグの組。</summary>
public readonly record struct KeyDef(ushort VirtualKey, bool IsExtended);

/// <summary>
/// 辞書 JSON の send トークンで使えるキー名 → 仮想キーの対応表。
/// docs/dictionary-schema.md の「使用可能なキー名」と一致させること。
/// </summary>
public static class VirtualKeyMap
{
    private static readonly Dictionary<string, KeyDef> Map = new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> Modifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ctrl", "Shift", "Alt", "Win",
    };

    static VirtualKeyMap()
    {
        for (var c = 'A'; c <= 'Z'; c++) Map[c.ToString()] = new KeyDef((ushort)c, false);
        for (var c = '0'; c <= '9'; c++) Map[c.ToString()] = new KeyDef((ushort)c, false);
        for (var i = 1; i <= 24; i++) Map["F" + i] = new KeyDef((ushort)(0x70 + i - 1), false);

        Add("Esc", 0x1B);
        Add("Escape", 0x1B);
        Add("Tab", 0x09);
        Add("Enter", 0x0D);
        Add("Space", 0x20);
        Add("Backspace", 0x08);
        Add("CapsLock", 0x14);
        Add("Pause", 0x13);
        Add("Insert", 0x2D, ext: true);
        Add("Delete", 0x2E, ext: true);
        Add("Home", 0x24, ext: true);
        Add("End", 0x23, ext: true);
        Add("PageUp", 0x21, ext: true);
        Add("PageDown", 0x22, ext: true);
        Add("Up", 0x26, ext: true);
        Add("Down", 0x28, ext: true);
        Add("Left", 0x25, ext: true);
        Add("Right", 0x27, ext: true);
        Add("PrintScreen", 0x2C, ext: true);
        Add("PrtScn", 0x2C, ext: true);
        Add("Apps", 0x5D, ext: true);
        Add("Menu", 0x5D, ext: true);
        Add("Comma", 0xBC);
        Add("Period", 0xBE);
        Add("Semicolon", 0xBA);
        Add("Slash", 0xBF);
        Add("Backslash", 0xDC);
        Add("Minus", 0xBD);
        Add("Plus", 0xBB);
        Add("Equals", 0xBB);
        Add("Grave", 0xC0);
        Add("LeftBracket", 0xDB);
        Add("RightBracket", 0xDD);
        Add("Quote", 0xDE);

        // 修飾キー単体タップ用（左側キーを使用。Win は拡張キー）
        Add("Ctrl", 0xA2);
        Add("Shift", 0xA0);
        Add("Alt", 0xA4);
        Add("Win", 0x5B, ext: true);
    }

    private static void Add(string name, ushort vk, bool ext = false) => Map[name] = new KeyDef(vk, ext);

    public static bool TryGet(string name, out KeyDef def) => Map.TryGetValue(name.Trim(), out def);

    public static bool IsModifier(string name) => Modifiers.Contains(name.Trim());
}
