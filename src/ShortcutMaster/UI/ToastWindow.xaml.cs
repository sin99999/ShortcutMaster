using System.Windows;
using System.Windows.Threading;
using ShortcutMaster.Interop;

namespace ShortcutMaster.UI;

/// <summary>失敗・案内メッセージを短時間だけ表示する小さなトースト。</summary>
public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _hideTimer;

    public ToastWindow()
    {
        InitializeComponent();
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2800) };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            Hide();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NonActivatingWindow.Apply(this, clickThrough: true);
    }

    public void ShowMessage(string message, Window anchor)
    {
        MessageText.Text = message;
        Show();
        UpdateLayout();

        var workArea = SystemParameters.WorkArea;
        Left = Math.Max(workArea.Left + 8, anchor.Left + anchor.ActualWidth - ActualWidth);
        Top = Math.Max(workArea.Top + 8, anchor.Top - ActualHeight - 8);

        _hideTimer.Stop();
        _hideTimer.Start();
    }
}
