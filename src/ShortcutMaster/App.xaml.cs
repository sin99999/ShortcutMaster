using System.IO;
using System.Windows;
using System.Windows.Threading;
using ShortcutMaster.Core.Models;
using ShortcutMaster.Core.Parsing;
using ShortcutMaster.Core.Services;
using ShortcutMaster.Interop;
using ShortcutMaster.UI;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace ShortcutMaster;

public partial class App : Application
{
    private const string MutexName = "ShortcutMaster_SingleInstance";

    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;
    private volatile bool _exiting;
    private int _cleanedUp;

    private ForegroundTracker? _tracker;
    private KeyInjector? _injector;
    private UsageStore? _usage;
    private List<ShortcutDictionary> _dictionaries = new();

    private ChipWindow? _chip;
    private PanelWindow? _panel;
    private ToastWindow? _toast;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private DispatcherTimer? _refreshDebounce;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(false, MutexName, out _);
        try
        {
            _ownsMutex = _singleInstanceMutex.WaitOne(0, false);
        }
        catch (AbandonedMutexException)
        {
            _ownsMutex = true;
        }

        if (!_ownsMutex)
        {
            MessageBox.Show("ShortcutMaster は既に起動しています。", "ShortcutMaster",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var loadErrors = new List<string>();
        _dictionaries = DictionaryLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "Data"), loadErrors);
        _usage = new UsageStore(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShortcutMaster", "usage.json"));
        _injector = new KeyInjector();

        _chip = new ChipWindow();
        _panel = new PanelWindow();
        _panel.AttachChip(_chip);
        _chip.Clicked += TogglePanel;

        SetupTrayIcon();

        _tracker = new ForegroundTracker();
        _refreshDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _refreshDebounce.Tick += (_, _) =>
        {
            _refreshDebounce!.Stop();
            UpdateContext();
        };
        _tracker.Changed += _ =>
        {
            if (_exiting) return;
            _refreshDebounce!.Stop();
            _refreshDebounce.Start();
        };
        _tracker.Start();

        _chip.Show();
        UpdateContext();

        if (_dictionaries.Count == 0)
        {
            MessageBox.Show(
                "辞書ファイル（Data\\*.json）を読み込めませんでした。アプリを配置し直してください。",
                "ShortcutMaster", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else if (loadErrors.Count > 0)
        {
            ShowToast("一部の辞書を読み込めませんでした。");
        }

        AppDomain.CurrentDomain.ProcessExit += (_, _) => CleanupResources();
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = AppIcon.LoadTrayIcon(),
            Visible = true,
            Text = "ShortcutMaster",
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("一覧を開く / 閉じる", null, (_, _) => Dispatcher.Invoke(TogglePanel));
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => Dispatcher.Invoke(ExitApplication));
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => Dispatcher.Invoke(TogglePanel);
    }

    private ShortcutDictionary? ResolveCurrentDictionary()
        => DictionaryLoader.ResolveForProcess(_dictionaries, _tracker?.Current?.ProcessName)
           ?? _dictionaries.FirstOrDefault();

    private void UpdateContext()
    {
        if (_exiting) return;

        var dictionary = ResolveCurrentDictionary();
        if (dictionary == null || _chip == null) return;

        _chip.SetContext(dictionary.DisplayName);
        if (_panel is { IsVisible: true })
            RefreshPanel(dictionary);
    }

    private void RefreshPanel(ShortcutDictionary dictionary)
    {
        if (_panel == null || _usage == null) return;

        var windowsCommon = dictionary.IsFallback ? null : _dictionaries.FirstOrDefault(d => d.IsFallback);
        var groups = PanelGroupBuilder.Build(dictionary, windowsCommon, _usage.GetCount);
        _panel.SetContent(dictionary.DisplayName, groups);
    }

    public void TogglePanel()
    {
        if (_exiting || _panel == null) return;

        if (_panel.IsVisible)
        {
            _panel.Hide();
            return;
        }

        var dictionary = ResolveCurrentDictionary();
        if (dictionary == null) return;

        RefreshPanel(dictionary);
        _panel.ShowNearChip();
    }

    public void ShowToast(string message)
    {
        if (_exiting) return;

        _toast ??= new ToastWindow();
        Window anchor = _panel is { IsVisible: true } ? _panel : _chip!;
        _toast.ShowMessage(message, anchor);
    }

    /// <summary>パネルの行クリックから呼ばれる。直前の前面アプリへショートカットを送信する。</summary>
    public async Task ExecuteEntryAsync(ShortcutEntry entry)
    {
        if (_exiting || _injector == null || _usage == null) return;

        if (_tracker?.Current is not { } target)
        {
            ShowToast("対象のウィンドウが見つかりません。");
            return;
        }

        if (!string.IsNullOrWhiteSpace(entry.FocusProcess))
        {
            var activate = await Task.Run(() => ProcessWindowActivator.TryActivate(entry.FocusProcess!));
            switch (activate)
            {
                case ActivateOutcome.Activated:
                    _ = Task.Run(() => _usage.Increment(entry.Id));
                    return;
                case ActivateOutcome.Failed:
                    ShowToast("起動中ですが前面に表示できません。管理者権限の違いをご確認ください。");
                    return;
            }
        }

        IReadOnlyList<ChordStep> steps;
        try
        {
            steps = ChordParser.ParseSequence(entry.Send!);
        }
        catch
        {
            ShowToast("このショートカットは送信できません。");
            return;
        }

        var outcome = await _injector.SendAsync(target, steps);
        if (outcome.Success)
        {
            var id = entry.Id;
            _ = Task.Run(() => _usage.Increment(id));
        }
        else
        {
            ShowToast(outcome.Message);
        }
    }

    private void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;

        _refreshDebounce?.Stop();

        // 見えるものを先に消す（終了のキビキビ感）
        _panel?.Hide();
        _chip?.Hide();
        _toast?.Hide();
        if (_notifyIcon != null)
            _notifyIcon.Visible = false;

        Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        CleanupResources();
        base.OnExit(e);
    }

    private void CleanupResources()
    {
        if (Interlocked.Exchange(ref _cleanedUp, 1) == 1) return;

        _refreshDebounce?.Stop();
        _refreshDebounce = null;

        _tracker?.Dispose();
        _tracker = null;
        _injector?.Dispose();
        _injector = null;
        _usage?.Dispose();
        _usage = null;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Icon?.Dispose();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        if (_ownsMutex)
        {
            try { _singleInstanceMutex?.ReleaseMutex(); } catch { }
            _ownsMutex = false;
        }
        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        base.OnSessionEnding(e);
        ExitApplication();
    }
}
