using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
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
    private int _shortcutBusy;

    private ForegroundTracker? _tracker;
    private KeyInjector? _injector;
    private UsageStore? _usage;
    private List<ShortcutDictionary> _dictionaries = new();

    private ChipWindow? _chip;
    private PanelWindow? _panel;
    private ToastWindow? _toast;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private OutsideClickWatcher? _outsideClick;
    private DispatcherTimer? _refreshDebounce;
    private DispatcherTimer? _overlayRestoreTimer;
    private DispatcherTimer? _chipHeartbeat;
    private bool _chipHiddenForOverlay;
    private DateTime _overlayHiddenAtUtc;
    private DateTime _lastPanelToggleUtc;

    /// <summary>範囲選択が続くオーバーレイ（閉じるまで UI を隠す）。</summary>
    private static readonly HashSet<string> InteractiveOverlayEntryIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "win.snip",
        "win.text-extract",
        "win.print-screen",
        "win.screen-record",
        "win.color-picker",
        "win.game-bar",
        "win.game-record",
    };

    /// <summary>瞬間キャプチャ（隠れてもすぐ復帰してよい）。</summary>
    private static readonly HashSet<string> InstantCaptureEntryIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "win.full-screenshot",
        "win.window-screenshot",
    };

    /// <summary>切り取り・OCR など、復帰を遅らせる前面プロセス名。</summary>
    private static readonly HashSet<string> OverlayHostProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ScreenClippingHost",
        "SnippingTool",
        "ShellExperienceHost",
        "TextInputHost",
        "GameBar",
        "GameBarFTServer",
        "XboxPcAppFT",
    };

    /// <summary>シェル UI / デスクトップ操作後に Topmost や最小化が乱れやすいもの。</summary>
    private static readonly HashSet<string> ReassertShellEntryIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "win.show-desktop",
        "win.minimize-all",
        "win.minimize-others",
        "win.start",
        "win.search",
        "win.quick-settings",
        "win.notification-center",
        "win.widgets",
        "win.project",
        "win.cast",
        "win.copilot",
        "win.task-view",
        "win.snap-layouts",
        "win.alt-tab",
        "win.task-switcher",
        "win.clipboard-history",
        "win.emoji",
        "win.voice-typing",
        "win.input-switch",
        "win.magnifier",
        "win.vd-new",
        "win.vd-left",
        "win.vd-right",
        "win.vd-close",
        "win.fullscreen",
        "win.always-on-top",
        "win.run",
        "win.quicklink",
    };

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
        _outsideClick = new OutsideClickWatcher(
            Dispatcher,
            () => new Window?[] { _panel, _chip, _toast },
            CollapsePanelToChip);
        _outsideClick.ArmFailed += () =>
            Dispatcher.BeginInvoke(() => ShowToast("外側クリックで閉じる機能を開始できませんでした。"));

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
            if (_panel is { IsVisible: true })
            {
                UpdateContext();
                return;
            }

            _refreshDebounce!.Stop();
            _refreshDebounce.Start();
        };
        _tracker.Start();

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        EnsureChipResident();
        UpdateContext();

        _chipHeartbeat = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _chipHeartbeat.Tick += (_, _) => EnsureChipResident();
        _chipHeartbeat.Start();

        if (!_tracker.IsHookActive)
            ShowToast("前面アプリの検知を開始できませんでした。再起動をお試しください。");

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
        menu.Items.Add("右下のチップを表示", null, (_, _) => Dispatcher.Invoke(ForceShowChipResident));
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => Dispatcher.Invoke(ExitApplication));
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => Dispatcher.Invoke(() =>
        {
            ForceShowChipResident();
            TogglePanel();
        });
    }

    /// <summary>起動中は右下チップを常に見せる（一覧の開閉とは独立）。</summary>
    public void EnsureChipResident()
    {
        if (_exiting || _chip == null || _chipHiddenForOverlay) return;
        _chip.EnsureResidentVisible();
    }

    /// <summary>トレイから強制復帰（オーバーレイ非表示フラグも解除）。</summary>
    public void ForceShowChipResident()
    {
        if (_exiting) return;
        RestoreUiAfterOverlay();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(ForceShowChipResident);

    private void HideUiForOverlayCapture()
    {
        _chipHiddenForOverlay = true;
        _overlayHiddenAtUtc = DateTime.UtcNow;
        // OCR / 切り取り中はグローバルマウスフックを外す
        _outsideClick?.Disarm();
        _panel?.Hide();
        _chip?.Hide();
        _toast?.Hide();

        _overlayRestoreTimer?.Stop();
        _overlayRestoreTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _overlayRestoreTimer.Tick += (_, _) =>
        {
            _overlayRestoreTimer?.Stop();
            RestoreUiAfterOverlay();
        };
        _overlayRestoreTimer.Start();
    }

    private void RestoreUiAfterOverlay()
    {
        if (_exiting) return;
        _overlayRestoreTimer?.Stop();
        _chipHiddenForOverlay = false;
        _chip?.EnsureResidentVisible();
    }

    /// <summary>
    /// OCR などのオーバーレイが閉じたあとにチップを戻す。
    /// 直後の前面切替はオーバーレイ自身の起動なので、少し待ってから復帰する。
    /// </summary>
    private void TryRestoreUiAfterOverlayIfReady()
    {
        if (!_chipHiddenForOverlay) return;
        if ((DateTime.UtcNow - _overlayHiddenAtUtc).TotalMilliseconds < 1500) return;

        var fg = _tracker?.Current?.ProcessName;
        if (!string.IsNullOrEmpty(fg) && OverlayHostProcessNames.Contains(fg))
            return;

        RestoreUiAfterOverlay();
    }

    /// <summary>OS 側の Desktop/シェル操作後に、遅延で最前面を取り戻す。</summary>
    private void ScheduleChipReassert(params int[] delaysMs)
    {
        foreach (var ms in delaysMs)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                EnsureChipResident();
            };
            timer.Start();
        }
    }

    public bool TryBeginShortcut()
        => !_exiting && Interlocked.CompareExchange(ref _shortcutBusy, 1, 0) == 0;

    public void EndShortcut() => Interlocked.Exchange(ref _shortcutBusy, 0);

    private ShortcutDictionary? ResolveCurrentDictionary()
        => DictionaryLoader.ResolveForProcess(_dictionaries, _tracker?.Current?.ProcessName)
           ?? _dictionaries.FirstOrDefault();

    private ShortcutDictionary? ResolveWindowsCommon()
        => _dictionaries.FirstOrDefault(d => d.IsFallback);

    private void UpdateContext()
    {
        if (_exiting) return;

        TryRestoreUiAfterOverlayIfReady();

        var dictionary = ResolveCurrentDictionary();
        if (dictionary == null || _chip == null) return;

        _chip.SetContext(dictionary.DisplayName);

        // 前面アプリ切替のたびに最前面を取り戻す（OCR 後に下に沈む対策）
        if (!_chipHiddenForOverlay)
            EnsureChipResident();

        if (_panel is { IsVisible: true })
            RefreshPanel(dictionary);
    }

    private void RefreshPanel(ShortcutDictionary dictionary)
    {
        if (_panel == null || _usage == null) return;

        var windowsCommon = dictionary.IsFallback ? null : ResolveWindowsCommon();
        var catalog = PanelCatalogBuilder.Build(dictionary, windowsCommon, _usage.GetCount);
        var groups = catalog.Select(g => new GroupVm
        {
            Name = g.Name,
            Rows = g.Entries.Select(e => new RowVm { Entry = e }).ToList(),
        }).ToList();
        _panel.SetContent(dictionary.DisplayName, groups);
    }

    public void TogglePanel()
    {
        if (_exiting || _panel == null) return;

        // ダブルクリックで開いて即閉じるのを抑止
        var now = DateTime.UtcNow;
        if ((now - _lastPanelToggleUtc).TotalMilliseconds < 320) return;
        _lastPanelToggleUtc = now;

        EnsureChipResident();

        if (_panel.IsVisible)
        {
            CollapsePanelToChip();
            return;
        }

        var dictionary = ResolveCurrentDictionary();
        if (dictionary == null) return;

        RefreshPanel(dictionary);
        _panel.ShowNearChip();
        _outsideClick?.Arm();
    }

    /// <summary>一覧を閉じて右下チップだけにする（− / ✕ / 範囲外クリック共通）。</summary>
    public void CollapsePanelToChip()
    {
        _outsideClick?.Disarm();
        if (_panel is { IsVisible: true })
            _panel.Hide();
        EnsureChipResident();
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

        var dictionary = ResolveCurrentDictionary();
        if (dictionary == null)
        {
            ShowToast("対象のウィンドウが見つかりません。");
            return;
        }

        if (!DictionaryLoader.EntryBelongsToContext(entry, dictionary, ResolveWindowsCommon()))
        {
            ShowToast("表示中のアプリが切り替わりました。一覧を開き直してください。");
            return;
        }

        if (entry.HasFocusProcess)
        {
            var activate = await Task.Run(() => ProcessWindowActivator.TryActivate(entry.FocusProcess!));
            if (_exiting) return;

            switch (activate)
            {
                case ActivateOutcome.Activated:
                    _usage.Increment(entry.Id);
                    return;
                case ActivateOutcome.FailedElevated:
                    ShowToast("管理者権限で起動中のため前面に表示できません。");
                    return;
                case ActivateOutcome.FailedForeground:
                case ActivateOutcome.NotRunning:
                    // 前面化できない／未起動 → 下の send（ホットキー）で起動・再表示を試みる
                    break;
            }
        }

        if (entry.Send is not { Length: > 0 })
        {
            ShowToast(entry.HasFocusProcess
                ? "対象のプロセスは起動していません。"
                : "このショートカットは送信できません。");
            return;
        }

        IReadOnlyList<ChordStep> steps;
        try
        {
            steps = ChordParser.ParseSequence(entry.Send);
        }
        catch
        {
            ShowToast("このショートカットは送信できません。");
            return;
        }

        var isInteractiveOverlay = InteractiveOverlayEntryIds.Contains(entry.Id);
        var isInstantCapture = InstantCaptureEntryIds.Contains(entry.Id);
        if (isInteractiveOverlay || isInstantCapture)
            HideUiForOverlayCapture();

        var outcome = await _injector.SendAsync(target, steps);
        if (_exiting) return;

        if (outcome.Success)
        {
            _usage.Increment(entry.Id);
            if (isInstantCapture)
            {
                // 瞬間キャプチャは前面切替が無いことが多い → すぐ復帰＋遅延でもう一押し
                RestoreUiAfterOverlay();
                ScheduleChipReassert(400);
            }
            else if (isInteractiveOverlay)
            {
                // OCR / 切り取りは前面切替 or タイムアウトで復帰
            }
            else if (ReassertShellEntryIds.Contains(entry.Id))
            {
                EnsureChipResident();
                ScheduleChipReassert(350, 1200);
            }
            else
            {
                EnsureChipResident();
            }
        }
        else
        {
            if (isInteractiveOverlay || isInstantCapture)
                RestoreUiAfterOverlay();
            ShowToast(outcome.Message);
        }
    }

    public void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;

        _refreshDebounce?.Stop();
        _chipHeartbeat?.Stop();
        _outsideClick?.Disarm();

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
        _overlayRestoreTimer?.Stop();
        _overlayRestoreTimer = null;
        _chipHeartbeat?.Stop();
        _chipHeartbeat = null;
        _outsideClick?.Dispose();
        _outsideClick = null;
        _chipHiddenForOverlay = false;

        try { SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; } catch { }

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
