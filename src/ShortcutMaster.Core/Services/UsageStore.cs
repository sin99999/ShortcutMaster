using System.Text.Json;

namespace ShortcutMaster.Core.Services;

/// <summary>
/// ショートカットの使用回数を JSON ファイルに保存する。
/// 保存するのは「エントリ id → 回数」のみで、入力内容は一切記録しない。
/// </summary>
public sealed class UsageStore : IDisposable
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly object _saveGate = new();
    private Dictionary<string, int> _counts = new(StringComparer.OrdinalIgnoreCase);
    private Timer? _saveTimer;
    private bool _disposed;

    private const int SaveDebounceMs = 500;

    public UsageStore(string path)
    {
        _path = path;
        Load();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var data = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(_path));
            if (data != null)
                _counts = new Dictionary<string, int>(data, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            _counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public int GetCount(string id)
    {
        lock (_gate)
        {
            return _counts.TryGetValue(id, out var count) ? count : 0;
        }
    }

    public void Increment(string id)
    {
        lock (_gate)
        {
            _counts[id] = (_counts.TryGetValue(id, out var count) ? count : 0) + 1;
        }
        ScheduleSave();
    }

    /// <summary>保留中の保存を即時実行する（テスト・終了時用）。</summary>
    public void FlushPending()
    {
        lock (_saveGate)
        {
            _saveTimer?.Dispose();
            _saveTimer = null;
        }
        SaveAtomic();
    }

    private void ScheduleSave()
    {
        lock (_saveGate)
        {
            if (_disposed) return;
            _saveTimer?.Dispose();
            _saveTimer = new Timer(_ => SaveAtomic(), null, SaveDebounceMs, Timeout.Infinite);
        }
    }

    private void SaveAtomic()
    {
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string json;
            lock (_gate)
            {
                json = JsonSerializer.Serialize(_counts);
            }

            var tempPath = _path + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _path, overwrite: true);
        }
        catch
        {
            // 保存失敗は致命的ではない（次回の保存で回復する）
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        FlushPending();
        lock (_saveGate)
        {
            _saveTimer?.Dispose();
            _saveTimer = null;
        }
    }
}
