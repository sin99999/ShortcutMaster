using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ShortcutMaster.Interop;

namespace ShortcutMaster.UI;

/// <summary>画面右下に常駐する極小チップ。クリックでパネルを開閉する。</summary>
public partial class ChipWindow : Window
{
    private const int SwRestore = 9;

    public event Action? Clicked;

    public ChipWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => EnsureResidentVisible();
        SizeChanged += (_, _) => PositionBottomRight();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NonActivatingWindow.Apply(this);
    }

    private void OnChipClicked(object sender, MouseButtonEventArgs e) => Clicked?.Invoke();

    private void OnExitClicked(object sender, RoutedEventArgs e)
        => ((App)System.Windows.Application.Current).ExitApplication();

    public void SetContext(string displayName)
    {
        ChipLabel.Text = $"{displayName} のショートカット";
        Dispatcher.BeginInvoke(PositionBottomRight, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>常駐中は必ず右下に表示し、他ウィンドウの下に沈まないようにする。</summary>
    public void EnsureResidentVisible()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero && NativeMethods.IsIconic(handle))
            NativeMethods.ShowWindow(handle, SwRestore);

        if (WindowState != WindowState.Normal)
            WindowState = WindowState.Normal;

        if (!IsVisible)
            Show();

        Topmost = false;
        Topmost = true;
        PositionBottomRight();
        ForceTopmost();
    }

    private void PositionBottomRight()
    {
        UpdateLayout();
        var workArea = SystemParameters.WorkArea;
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        if (double.IsNaN(width) || width <= 0) width = 220;
        if (double.IsNaN(height) || height <= 0) height = 32;

        Left = workArea.Right - width - 12;
        Top = workArea.Bottom - height - 8;
    }

    private void ForceTopmost()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;

        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HwndTopmost,
            0, 0, 0, 0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
    }
}
