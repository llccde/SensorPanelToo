using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SensorPanelToo.Controls;
using SensorPanelToo.Models;
using SensorPanelToo.Services;

namespace SensorPanelToo.Views;

public partial class RenderWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private readonly string _configPath;
    private readonly int _monitorIndex;
    private bool _fullscreen;
    private DashboardConfig _config = new();
    private readonly Dictionary<Guid, UserControl> _controlMap = new();
    private readonly DispatcherTimer _sensorTimer = new();
    private readonly DispatcherTimer _chartTimer = new();
    private bool _layoutApplied;

    public RenderWindow(string configPath, bool fullscreen = false, int monitorIndex = 0)
    {
        _configPath = configPath;
        _fullscreen = fullscreen;
        _monitorIndex = monitorIndex;
        InitializeComponent();
        LoadConfig();
        RenderAllComponents();
    }

    void LoadConfig()
    {
        try { _config = ConfigService.Load(_configPath); }
        catch (Exception ex)
        {
            MessageBox.Show($"加载主题失败：{_configPath}\n{ex.Message}",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_layoutApplied) return;
        _layoutApplied = true;

        ApplyWindowLayout();
        InitTimers();
        Activate();
    }

    void ApplyWindowLayout()
    {
        var canvasColor = ParseColor(_config.BackgroundColor);
        var bgBrush = new SolidColorBrush(canvasColor);
        RenderCanvas.Background = bgBrush;
        ViewboxBorder.Background = bgBrush;
        Background = bgBrush;

        if (_fullscreen)
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            int idx = Math.Clamp(_monitorIndex, 0, screens.Length - 1);
            var screen = screens[idx];

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            WindowState = WindowState.Normal;

            var hwnd = new WindowInteropHelper(this).EnsureHandle();
            SetWindowPos(hwnd, HWND_TOPMOST,
                screen.Bounds.Left, screen.Bounds.Top,
                screen.Bounds.Width, screen.Bounds.Height,
                SWP_SHOWWINDOW);
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Topmost = false;
            ShowInTaskbar = true;
            Width = Math.Min(_config.CanvasWidth, System.Windows.SystemParameters.WorkArea.Width * 0.9);
            Height = Math.Min(_config.CanvasHeight, System.Windows.SystemParameters.WorkArea.Height * 0.9);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Title = $"SensorPanel - {_config.ThemeName}";
        }
    }

    void InitTimers()
    {
        _sensorTimer.Interval = TimeSpan.FromMilliseconds(500);
        _sensorTimer.Tick += (_, _) => PushLiveSensorData();
        _sensorTimer.Start();

        _chartTimer.Interval = TimeSpan.FromMilliseconds(500);
        _chartTimer.Tick += (_, _) => AppendChartHistories();
        _chartTimer.IsEnabled = _config.Components.Any(c => c.ComponentType == ComponentType.GridChart);
        if (_chartTimer.IsEnabled) _chartTimer.Start();
    }

    void RenderAllComponents()
    {
        RenderCanvas.Width = _config.CanvasWidth;
        RenderCanvas.Height = _config.CanvasHeight;
        _controlMap.Clear();
        RenderCanvas.Children.Clear();
        RenderCanvas.Children.Add(BgImage);
        RenderBackground();

        foreach (var comp in _config.Components)
        {
            var ctrl = CreateControl(comp);
            _controlMap[comp.Id] = ctrl;
            RenderCanvas.Children.Add(ctrl);
        }
    }

    UserControl CreateControl(Component comp)
    {
        UserControl ctrl = comp switch
        {
            CircularGaugeComponent cgc => new CircularGaugeControl { ComponentData = cgc },
            ProgressBarComponent pbc => new ProgressBarControl { ComponentData = pbc },
            DigitalDisplayComponent ddc => new DigitalDisplayControl { ComponentData = ddc },
            GridChartComponent gcc => new GridChartControl { ComponentData = gcc },
            SensorLabelComponent slc => new SensorLabelControl { ComponentData = slc },
            _ => throw new ArgumentException($"Unknown type: {comp.ComponentType}")
        };

        var (bw, bh) = GetBaseSize(comp.ComponentType);
        ctrl.Width = bw;
        ctrl.Height = bh;
        ctrl.RenderTransform = new ScaleTransform(comp.Scale, comp.Scale);
        ctrl.RenderTransformOrigin = new Point(0, 0);
        Canvas.SetLeft(ctrl, comp.X);
        Canvas.SetTop(ctrl, comp.Y);
        Canvas.SetZIndex(ctrl, comp.ZIndex);
        return ctrl;
    }

    static (double w, double h) GetBaseSize(ComponentType type) => type switch
    {
        ComponentType.ProgressBar => (300, 40),
        ComponentType.CircularGauge => (200, 200),
        ComponentType.DigitalDisplay => (250, 80),
        ComponentType.GridChart => (400, 200),
        ComponentType.SensorLabel => (300, 60),
        _ => (200, 200)
    };

    void PushLiveSensorData()
    {
        foreach (var comp in _config.Components.Where(c => c.BindingId != null))
        {
            var sv = HardwareService.Instance.GetSensor(comp.BindingId!);
            if (sv == null) continue;
            if (_controlMap.TryGetValue(comp.Id, out var ctrl))
                PushValue(ctrl, comp, sv);
        }
    }

    void AppendChartHistories()
    {
        foreach (var comp in _config.Components)
        {
            if (comp is not GridChartComponent gc) continue;
            float v = gc.BindingId != null
                && _controlMap.TryGetValue(gc.Id, out var ctrl)
                && ctrl is GridChartControl gcc
                && gcc.SensorValue != null
                    ? gcc.SensorValue.CurrentValue
                    : 0;
            gc.HistoryValues.Add((DateTime.Now, v));
            while (gc.HistoryValues.Count > 0
                && (DateTime.Now - gc.HistoryValues[0].Time).TotalSeconds > gc.DurationSeconds * 2)
                gc.HistoryValues.RemoveAt(0);
            if (_controlMap.TryGetValue(gc.Id, out var gcCtrl))
                gcCtrl.InvalidateVisual();
        }
    }

    static void PushValue(UserControl ctrl, Component comp, SensorValue sv)
    {
        if (ctrl is ProgressBarControl pb) { pb.SensorValue = sv; pb.RefreshValue(); }
        else if (ctrl is CircularGaugeControl cg) { cg.SensorValue = sv; cg.InvalidateVisual(); }
        else if (ctrl is DigitalDisplayControl dd) { dd.SensorValue = sv; dd.Refresh(); }
        else if (ctrl is GridChartControl gc) { gc.SensorValue = sv; gc.InvalidateVisual(); }
        else if (ctrl is SensorLabelControl sl) { sl.SensorValue = sv; sl.Refresh(); }
    }

    void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    void RenderBackground()
    {
        try { RenderCanvas.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_config.BackgroundColor)); }
        catch { RenderCanvas.Background = new SolidColorBrush(Colors.Black); }

        string path = _config.BackgroundImagePath;
        if (!string.IsNullOrEmpty(path))
        {
            if (!Path.IsPathRooted(path))
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            if (File.Exists(path))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                BgImage.Source = bmp;
                BgImage.Visibility = Visibility.Visible;
                BgImage.RenderTransform = new ScaleTransform(_config.BackgroundImageScale, _config.BackgroundImageScale);
                BgImage.RenderTransformOrigin = new Point(0, 0);
                Canvas.SetLeft(BgImage, _config.BackgroundImageOffsetX);
                Canvas.SetTop(BgImage, _config.BackgroundImageOffsetY);
                return;
            }
        }
        BgImage.Source = null;
        BgImage.Visibility = Visibility.Collapsed;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _sensorTimer.Stop();
        _chartTimer.Stop();
        base.OnClosing(e);
    }

    static Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Colors.Black; }
    }
}
