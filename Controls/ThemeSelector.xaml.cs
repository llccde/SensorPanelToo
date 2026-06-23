using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SensorPanelToo.Models;
using SensorPanelToo.Services;

namespace SensorPanelToo.Controls;

public partial class ThemeSelector : UserControl
{
    public static readonly RoutedEvent ThemeSelectedEvent =
        EventManager.RegisterRoutedEvent(nameof(ThemeSelected), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(ThemeSelector));

    public event RoutedEventHandler ThemeSelected
    {
        add => AddHandler(ThemeSelectedEvent, value);
        remove => RemoveHandler(ThemeSelectedEvent, value);
    }

    public static readonly RoutedEvent ThemeDoubleClickedEvent =
        EventManager.RegisterRoutedEvent(nameof(ThemeDoubleClicked), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(ThemeSelector));

    public event RoutedEventHandler ThemeDoubleClicked
    {
        add => AddHandler(ThemeDoubleClickedEvent, value);
        remove => RemoveHandler(ThemeDoubleClickedEvent, value);
    }

    private List<string> _themeNames = new();
    private bool _ready;

    public string? SelectedThemeName => ThemeListBox.SelectedItem as string;

    public string? SelectedThemePath
    {
        get
        {
            var name = SelectedThemeName;
            if (name == null) return null;
            var path = Path.Combine(ConfigService.ThemesDirectory, name + ".json");
            return File.Exists(path) ? path : null;
        }
    }

    public ThemeSelector()
    {
        InitializeComponent();
        RefreshList();
        _ready = true;
    }

    public void RefreshList()
    {
        _themeNames = ConfigService.ListThemes();
        ThemeListBox.ItemsSource = null;
        ThemeListBox.ItemsSource = _themeNames;
        ThemeCountLabel.Text = $"共 {_themeNames.Count} 个主题";
    }

    public void SelectTheme(string name)
    {
        int idx = _themeNames.IndexOf(name);
        if (idx >= 0)
            ThemeListBox.SelectedIndex = idx;
    }

    void ThemeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        RefreshPreview();
        RaiseEvent(new RoutedEventArgs(ThemeSelectedEvent));
    }

    void ThemeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedThemeName != null)
            RaiseEvent(new RoutedEventArgs(ThemeDoubleClickedEvent));
    }

    void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        RefreshList();
        RefreshPreview();
    }

    void RenderBackgroundImage(DashboardConfig config)
    {
        string path = config.BackgroundImagePath;
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
                BgImage.RenderTransform = new ScaleTransform(config.BackgroundImageScale, config.BackgroundImageScale);
                BgImage.RenderTransformOrigin = new Point(0, 0);
                Canvas.SetLeft(BgImage, config.BackgroundImageOffsetX);
                Canvas.SetTop(BgImage, config.BackgroundImageOffsetY);
                return;
            }
        }
        BgImage.Source = null;
        BgImage.Visibility = Visibility.Collapsed;
    }

    void RefreshPreview()
    {
        PreviewCanvas.Children.Clear();
        PreviewCanvas.Children.Add(BgImage);
        var path = SelectedThemePath;
        if (path == null) return;

        try
        {
            var config = ConfigService.Load(path);
            PreviewCanvas.Width = config.CanvasWidth;
            PreviewCanvas.Height = config.CanvasHeight;
            PreviewCanvas.Background = new SolidColorBrush(ParseColor(config.BackgroundColor));

            RenderBackgroundImage(config);

            foreach (var comp in config.Components)
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
                PreviewCanvas.Children.Add(ctrl);
            }
        }
        catch { }
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

    static Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Colors.Black; }
    }
}
