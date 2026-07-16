using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ShortcutMaster.Interop;

namespace ShortcutMaster.UI;

/// <summary>チップの上に展開するショートカット一覧パネル。</summary>
public partial class PanelWindow : Window
{
    private ChipWindow? _chip;

    public PanelWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Reposition();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NonActivatingWindow.Apply(this);
    }

    public void AttachChip(ChipWindow chip) => _chip = chip;

    public void SetContent(string appDisplayName, List<GroupVm> groups)
    {
        HeaderTitle.Text = $"{appDisplayName} で今使えるショートカット";
        Scroller.MaxHeight = Math.Max(240, SystemParameters.WorkArea.Height * 0.62);
        GroupsList.ItemsSource = groups;
    }

    public void ShowNearChip()
    {
        Show();
        Reposition();
    }

    private void Reposition()
    {
        if (_chip == null || !IsVisible) return;

        var workArea = SystemParameters.WorkArea;
        UpdateLayout();
        Left = Math.Max(workArea.Left + 8, _chip.Left + _chip.ActualWidth - ActualWidth);
        Top = Math.Max(workArea.Top + 8, _chip.Top - ActualHeight - 8);
    }

    private void CollapseToChip()
        => ((App)System.Windows.Application.Current).CollapsePanelToChip();

    private void OnMinimizeClicked(object sender, RoutedEventArgs e) => CollapseToChip();

    private void OnCloseClicked(object sender, RoutedEventArgs e) => CollapseToChip();

    private void OnRowClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: RowVm row } element) return;

        var app = (App)System.Windows.Application.Current;
        if (!row.IsSendable)
        {
            app.ShowToast(row.NoteText ?? "このショートカットは表示のみです。");
            return;
        }

        if (!app.TryBeginShortcut())
            return;

        element.BeginAnimation(OpacityProperty, new DoubleAnimation(0.35, 1.0, TimeSpan.FromMilliseconds(240)));
        _ = ExecuteRowAsync(app, row);
    }

    private static async Task ExecuteRowAsync(App app, RowVm row)
    {
        try
        {
            await app.ExecuteEntryAsync(row.Entry);
        }
        finally
        {
            app.EndShortcut();
        }
    }
}
