using System.Windows;

namespace SensorPanelToo.Views;

public partial class ComponentEditorWindow : Window
{
    public ComponentEditorWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        DebugPanel.StopTimers();
        base.OnClosing(e);
    }
}
