using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SensorPanelToo.Models;
using SensorPanelToo.Services;
using SensorPanelToo.Views;

namespace SensorPanelToo;

public partial class MainWindow : Window
{
    private bool _ready;
    private RenderWindow? _renderWindow;

    public MainWindow()
    {
        InitializeComponent();
        PopulateMonitors();
        _ready = true;
    }

    void TabTheme_Click(object sender, RoutedEventArgs e)
    {
        TabTheme.IsEnabled = false;
        TabDebug.IsEnabled = true;
        PanelTheme.Visibility = Visibility.Visible;
        PanelDebug.Visibility = Visibility.Collapsed;
    }

    void TabDebug_Click(object sender, RoutedEventArgs e)
    {
        TabTheme.IsEnabled = true;
        TabDebug.IsEnabled = false;
        PanelTheme.Visibility = Visibility.Collapsed;
        PanelDebug.Visibility = Visibility.Visible;
    }

    void PopulateMonitors()
    {
        MonitorCombo.Items.Clear();
        var screens = System.Windows.Forms.Screen.AllScreens;
        for (int i = 0; i < screens.Length; i++)
        {
            var s = screens[i];
            string label = screens.Length == 1
                ? $"显示器 1 ({s.Bounds.Width}x{s.Bounds.Height})"
                : $"显示器 {i + 1} ({s.Bounds.Width}x{s.Bounds.Height}){(s.Primary ? " [主屏]" : "")}";
            MonitorCombo.Items.Add(new ComboBoxItem { Content = label, Tag = i });
        }
        MonitorCombo.SelectedIndex = 0;
    }

    int SelectedMonitorIndex
    {
        get
        {
            if (MonitorCombo.SelectedItem is ComboBoxItem item && item.Tag is int idx)
                return idx;
            return 0;
        }
    }

    bool IsFullscreen => FullscreenCheck.IsChecked ?? true;

    void StartHardwareIfNeeded()
    {
        if (!HardwareService.Instance.IsRunning)
        {
            new HardwareSelectDialog().ShowDialog();
            UpdateServiceStatus();
        }
    }

    void UpdateServiceStatus()
    {
        ServiceStatusLabel.Text = HardwareService.Instance.IsRunning
            ? "硬件服务：运行中"
            : "硬件服务：未启动";
    }

    void ThemeSelector_ThemeSelected(object sender, RoutedEventArgs e) { }
    void ThemeSelector_ThemeDoubleClicked(object sender, RoutedEventArgs e) => DoRender();

    void NewThemeBtn_Click(object sender, RoutedEventArgs e)
    {
        string? name = ShowInputDialog("输入新主题名称：", "新建主题");
        if (string.IsNullOrWhiteSpace(name)) return;

        string path = Path.Combine(ConfigService.ThemesDirectory, name + ".json");
        if (File.Exists(path))
        {
            MessageBox.Show($"主题「{name}」已存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var config = new DashboardConfig
            {
                Version = "1.0",
                ThemeName = name,
                CanvasWidth = 1280,
                CanvasHeight = 720,
                BackgroundColor = "#0A0A0F",
                Components = new List<Component>()
            };
            ConfigService.Save(config, path);
            ThemeSelector.RefreshList();
            ThemeSelector.SelectTheme(name);
            EditTheme(new ThemeEditorWindow(path));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void EditThemeBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = ThemeSelector.SelectedThemePath;
        if (path == null)
        {
            MessageBox.Show("请先选择一个主题。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        EditTheme(new ThemeEditorWindow(path));
    }

    void EditTheme(ThemeEditorWindow editor)
    {
        editor.Closed += (_, _) => ThemeSelector.RefreshList();
        editor.Show();
    }

    void RenderThemeBtn_Click(object sender, RoutedEventArgs e) => DoRender();

    void DoRender()
    {
        var path = ThemeSelector.SelectedThemePath;
        if (path == null)
        {
            MessageBox.Show("请先选择一个主题。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CloseRenderWindow();
        StartHardwareIfNeeded();
        _renderWindow = new RenderWindow(path, IsFullscreen, SelectedMonitorIndex);
        _renderWindow.Closed += (_, _) =>
        {
            _renderWindow = null;
            UpdateServiceStatus();
        };
        _renderWindow.Show();
    }

    void CloseRenderWindow()
    {
        if (_renderWindow != null)
        {
            try { _renderWindow.Close(); } catch { }
            _renderWindow = null;
        }
    }

    void StartServiceBtn_Click(object sender, RoutedEventArgs e)
    {
        new HardwareSelectDialog().ShowDialog();
        UpdateServiceStatus();
    }

    void FullscreenCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        MonitorCombo.IsEnabled = IsFullscreen;
    }

    void MonitorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    static string? ShowInputDialog(string prompt, string title)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 340,
            Height = 160,
            WindowStyle = WindowStyle.ToolWindow,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        var grid = new Grid { Margin = new Thickness(12) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new TextBlock { Text = prompt, FontSize = 13, Margin = new Thickness(0, 4, 0, 8) };
        Grid.SetRow(label, 0);
        grid.Children.Add(label);

        var textBox = new TextBox { FontSize = 14 };
        Grid.SetRow(textBox, 1);
        grid.Children.Add(textBox);
        textBox.Focus();

        var btnPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        Grid.SetRow(btnPanel, 2);
        string? result = null;
        var okBtn = new Button { Content = "确定", Width = 70, Height = 26, Margin = new Thickness(0, 0, 8, 0) };
        okBtn.Click += (_, _) => { result = textBox.Text; dialog.Close(); };
        textBox.KeyDown += (_, ke) => { if (ke.Key == Key.Enter) { result = textBox.Text; dialog.Close(); } };
        var cancelBtn = new Button { Content = "取消", Width = 70, Height = 26 };
        cancelBtn.Click += (_, _) => { dialog.Close(); };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        grid.Children.Add(btnPanel);
        dialog.Content = grid;
        dialog.ShowDialog();
        return result;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        CloseRenderWindow();
        DebugPanel.StopTimers();
        base.OnClosing(e);
    }
}
