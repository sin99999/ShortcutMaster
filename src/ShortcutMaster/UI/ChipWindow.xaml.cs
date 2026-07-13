using System.Windows;
using System.Windows.Input;
using ShortcutMaster.Interop;

namespace ShortcutMaster.UI;

/// <summary>画面右下に常駐する極小チップ。クリックでパネルを開閉する。</summary>
public partial class ChipWindow : Window
{
    public event Action? Clicked;

    public ChipWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PositionBottomRight();
        SizeChanged += (_, _) => PositionBottomRight();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NonActivatingWindow.Apply(this);
    }

    private void OnChipClicked(object sender, MouseButtonEventArgs e) => Clicked?.Invoke();

    public void SetContext(string displayName)
        => ChipLabel.Text = $"{displayName} のショートカット";

    private void PositionBottomRight()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 12;
        Top = workArea.Bottom - ActualHeight - 8;
    }
}
