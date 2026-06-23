using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SensorPanelToo.Controls;
using SensorPanelToo.Models;
using SensorPanelToo.Services;

namespace SensorPanelToo.Views;

public partial class ThemeEditorWindow : Window
{
    private readonly ObservableCollection<Component> _components = new();
    private readonly Dictionary<Guid, UserControl> _controlMap = new();
    private Guid? _selectedId;
    private bool _backgroundSelected;
    private readonly DispatcherTimer _sensorTimer = new();
    private readonly DispatcherTimer _chartTimer = new();
    private bool _ready;
    private bool _suppressSync;

    private bool _isDragging;
    private bool _isPanning;
    private Point _dragStartMouse;
    private Point _panStartMouse;
    private double _dragStartX;
    private double _dragStartY;
    private double _panStartScrollX;
    private double _panStartScrollY;

    private TextBox? _activeColorBox;
    private readonly ColorPickerPopup _colorPopup = new();
    private readonly Dictionary<TextBox, System.Windows.Shapes.Rectangle> _swatches = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private string? _currentFilePath;

    public ThemeEditorWindow(string? themePath = null)
    {
        InitializeComponent();
        InitSensors();
        InitColorTextBoxes();
        PropBgColor.TextChanged += (_, _) =>
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(PropBgColor.Text);
                BgColorSwatch.Fill = new SolidColorBrush(c);
                RenderCanvas.Background = new SolidColorBrush(c);
                BgColorSwatch.InvalidateVisual();
            }
            catch { }
        };
        WireAllPairs();
        PosXSlider.Maximum = RenderCanvas.Width;
        PosYSlider.Maximum = RenderCanvas.Height;
        ComponentListBox.ItemsSource = _components;
        _ready = true;
        UpdateSelectionUI();

        if (themePath != null && File.Exists(themePath))
        {
            LoadConfig(themePath);
            _currentFilePath = themePath;
            UpdateTitle();
        }
    }

    // ==================== Init ====================

    void InitSensors()
    {
        _sensorTimer.Interval = TimeSpan.FromMilliseconds(500);
        _sensorTimer.Tick += (_, _) => PushLiveSensorData();
        _chartTimer.Interval = TimeSpan.FromMilliseconds(500);
        _chartTimer.Tick += (_, _) => AppendAllChartHistories();
    }

    void InitColorTextBoxes()
    {
        _colorPopup.ColorChanged += hex =>
        {
            if (_activeColorBox == null) return;
            _activeColorBox.Text = hex;
            if (_activeColorBox == PropBgColor)
            {
                BgColorSwatch.Fill = new SolidColorBrush(ParseColor(hex));
                RenderCanvas.Background = new SolidColorBrush(ParseColor(hex));
            }
            else { UpdateSwatchColor(_activeColorBox); ApplyEditorToComponent(); RefreshSelectedControl(); }
        };
        _colorPopup.ColorSelected += hex =>
        {
            if (_activeColorBox == null) return;
            _activeColorBox.Text = hex;
            if (_activeColorBox == PropBgColor)
            {
                BgColorSwatch.Fill = new SolidColorBrush(ParseColor(hex));
                RenderCanvas.Background = new SolidColorBrush(ParseColor(hex));
            }
            else { UpdateSwatchColor(_activeColorBox); ApplyEditorToComponent(); RefreshSelectedControl(); }
        };
        _colorPopup.ColorCancelled += hex =>
        {
            if (_activeColorBox == null) return;
            _activeColorBox.Text = hex;
            if (_activeColorBox == PropBgColor)
            {
                BgColorSwatch.Fill = new SolidColorBrush(ParseColor(hex));
                RenderCanvas.Background = new SolidColorBrush(ParseColor(hex));
            }
            else { UpdateSwatchColor(_activeColorBox); ApplyEditorToComponent(); RefreshSelectedControl(); }
        };
        foreach (var b in new[] { PropForeground, PropBackground, PropProgressTextColor, PropGaugeTextColor, PropBorderColor, PropNeedleColor, PropStrokeColor, PropLineColor, PropGridLineColor, PropTrackColor, PropLabelStrokeColor })
        {
            b.GotFocus += (_, _) => { _activeColorBox = b; _colorPopup.SetCurrentColor(b.Text); _colorPopup.PlacementTarget = b; _colorPopup.IsOpen = true; };
            AttachSwatch(b);
        }
        PropBgColor.GotFocus += (_, _) => { _activeColorBox = PropBgColor; _colorPopup.SetCurrentColor(PropBgColor.Text); _colorPopup.PlacementTarget = PropBgColor; _colorPopup.IsOpen = true; };
    }

    void AttachSwatch(TextBox tb)
    {
        var parent = tb.Parent as Panel;
        if (parent == null) return;
        int idx = parent.Children.IndexOf(tb);
        parent.Children.RemoveAt(idx);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var swatch = new System.Windows.Shapes.Rectangle
        {
            Width = 16, Height = 16, Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0),
            Stroke = Brushes.LightGray, StrokeThickness = 0.5
        };
        UpdateSwatchFill(swatch, tb.Text);
        swatch.MouseLeftButtonDown += (_, _) =>
        {
            _activeColorBox = tb;
            _colorPopup.SetCurrentColor(tb.Text);
            _colorPopup.PlacementTarget = swatch;
            _colorPopup.IsOpen = true;
        };
        Grid.SetColumn(swatch, 0);
        grid.Children.Add(swatch);

        Grid.SetColumn(tb, 1);
        grid.Children.Add(tb);

        parent.Children.Insert(idx, grid);
        _swatches[tb] = swatch;
    }

    void UpdateSwatchColor(TextBox tb)
    {
        if (_swatches.TryGetValue(tb, out var swatch))
            UpdateSwatchFill(swatch, tb.Text);
    }

    static void UpdateSwatchFill(System.Windows.Shapes.Rectangle swatch, string hex)
    {
        try { swatch.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { }
    }

    void WireAllPairs()
    {
        WirePair(ScaleSlider, ScaleBox);
        WirePair(PosXSlider, PosXBox);
        WirePair(PosYSlider, PosYBox);
        WirePair(FontSizeSlider, FontSizeBox);
        WirePair(BorderThicknessSlider, BorderThicknessBox);
        WirePair(RoundnessSlider, RoundnessBox);
        WirePair(SweepAngleSlider, SweepAngleBox);
        WirePair(StartAngleSlider, StartAngleBox);
        WirePair(RingThicknessSlider, RingThicknessBox);
        WirePair(NeedleWidthSlider, NeedleWidthBox);
        WirePair(DecimalPlacesSlider, DecimalPlacesBox);
        WirePair(StrokeThicknessSlider, StrokeThicknessBox);
        WirePair(DurationSlider, DurationBox);
        WirePair(GridXSlider, GridXBox);
        WirePair(GridYSlider, GridYBox);
        WirePair(LineWidthSlider, LineWidthBox);
        WirePair(SmoothFactorSlider, SmoothFactorBox);
        WirePair(FillOpacitySlider, FillOpacityBox);
        WirePair(HierarchyLevelsSlider, HierarchyLevelsBox);
        WirePair(LabelStrokeThicknessSlider, LabelStrokeThicknessBox);
    }

    void WirePair(Slider sl, TextBox box) { sl.Tag = box; box.Tag = sl; }

    // ==================== Component Management ====================

    Component? GetSelectedComponent() => _selectedId.HasValue
        ? _components.FirstOrDefault(c => c.Id == _selectedId.Value)
        : null;

    UserControl? GetSelectedControl() => _selectedId.HasValue && _controlMap.TryGetValue(_selectedId.Value, out var ctrl) ? ctrl : null;

    void AddComponent(ComponentType type)
    {
        Component comp = type switch
        {
            ComponentType.CircularGauge => new CircularGaugeComponent { ForegroundColor = "#FF6644", RingThickness = 14, ShowCenterValue = true, X = 50 + _components.Count * 30, Y = 50 + _components.Count * 30 },
            ComponentType.ProgressBar => new ProgressBarComponent { ForegroundColor = "#00AA44", BackgroundColor = "#E0E0E0", ShowValueText = true, TransparentBackground = false, X = 50 + _components.Count * 30, Y = 50 + _components.Count * 30 },
            ComponentType.DigitalDisplay => new DigitalDisplayComponent { ForegroundColor = "#0066CC", StrokeColor = "#CCCCCC", StrokeThickness = 1, ShowSuffix = true, DecimalPlaces = 1, FontWeight = FontWeightOption.Bold, FontSize = 36, X = 50 + _components.Count * 30, Y = 50 + _components.Count * 30 },
            ComponentType.GridChart => new GridChartComponent { DurationSeconds = 30, GridDensityX = 5, GridDensityY = 4, GridLineColor = "#E0E0E0", LineColor = "#0066CC", LineWidth = 2, SmoothFactor = 0.3, X = 50 + _components.Count * 30, Y = 50 + _components.Count * 30 },
            ComponentType.SensorLabel => new SensorLabelComponent { ForegroundColor = "#00CC88", StrokeColor = "#CCCCCC", StrokeThickness = 1, HierarchyLevels = 3, ShowSuffix = true, FontWeight = FontWeightOption.Bold, FontSize = 18, X = 50 + _components.Count * 30, Y = 50 + _components.Count * 30 },
            _ => new CircularGaugeComponent()
        };
        _components.Add(comp);
        var ctrl = CreateControlForComponent(comp);
        _controlMap[comp.Id] = ctrl;
        RenderCanvas.Children.Add(ctrl);
        SelectComponent(comp.Id);
    }

    void RemoveComponent(Guid id)
    {
        if (_components.FirstOrDefault(c => c.Id == id) is not Component comp) return;
        if (_controlMap.TryGetValue(id, out var ctrl))
        {
            RenderCanvas.Children.Remove(ctrl);
            _controlMap.Remove(id);
        }
        _components.Remove(comp);
        if (_selectedId == id) SelectComponent(null);
    }

    UserControl CreateControlForComponent(Component comp)
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

    void RebuildAllControls()
    {
        RenderCanvas.Children.Clear();
        RenderCanvas.Children.Add(BgImage);
        _controlMap.Clear();
        foreach (var comp in _components)
        {
            var ctrl = CreateControlForComponent(comp);
            _controlMap[comp.Id] = ctrl;
            RenderCanvas.Children.Add(ctrl);
        }
        PushSimulatedToUnbound();
    }

    // ==================== Selection ====================

    void SelectComponent(Guid? id)
    {
        _selectedId = id;
        _backgroundSelected = false;
        if (id.HasValue)
        {
            var comp = GetSelectedComponent();
            if (comp != null)
            {
                int maxZ = _components.Count > 1 ? _components.Max(c => c.ZIndex) : 0;
                comp.ZIndex = maxZ + 1;
                if (_controlMap.TryGetValue(comp.Id, out var ctrl))
                    Canvas.SetZIndex(ctrl, comp.ZIndex);
            }
        }
        UpdateSelectionUI();
        if (_selectedId.HasValue) SyncEditorsFromComponent();
    }

    void UpdateSelectionUI()
    {
        bool hasSelection = _selectedId.HasValue && GetSelectedComponent() != null;

        if (_backgroundSelected)
        {
            PanelSelection.Visibility = Visibility.Collapsed;
            PanelBackground.Visibility = Visibility.Visible;
            NoSelectionLabel.Visibility = Visibility.Collapsed;
        }
        else if (hasSelection)
        {
            PanelSelection.Visibility = Visibility.Visible;
            PanelBackground.Visibility = Visibility.Collapsed;
            NoSelectionLabel.Visibility = Visibility.Collapsed;
        }
        else
        {
            PanelSelection.Visibility = Visibility.Collapsed;
            PanelBackground.Visibility = Visibility.Collapsed;
            NoSelectionLabel.Visibility = Visibility.Visible;
        }

        if (hasSelection)
        {
            var comp = GetSelectedComponent()!;
            SelectedInfo.Text = $"{comp.ComponentType}  [{comp.Id.ToString()[..8]}]";
            BoundSensorLabel.Text = comp.BindingId != null ? $"Sensor: {comp.BindingId}" : "No sensor bound";
        }

        PanelProgress.Visibility = hasSelection && GetSelectedComponent()?.ComponentType == ComponentType.ProgressBar ? Visibility.Visible : Visibility.Collapsed;
        PanelGauge.Visibility = hasSelection && GetSelectedComponent()?.ComponentType == ComponentType.CircularGauge ? Visibility.Visible : Visibility.Collapsed;
        PanelDisplay.Visibility = hasSelection && GetSelectedComponent()?.ComponentType == ComponentType.DigitalDisplay ? Visibility.Visible : Visibility.Collapsed;
        PanelChart.Visibility = hasSelection && GetSelectedComponent()?.ComponentType == ComponentType.GridChart ? Visibility.Visible : Visibility.Collapsed;
        PanelLabel.Visibility = hasSelection && GetSelectedComponent()?.ComponentType == ComponentType.SensorLabel ? Visibility.Visible : Visibility.Collapsed;

        _chartTimer.IsEnabled = _components.Any(c => c.ComponentType == ComponentType.GridChart);

        _suppressSync = true;
        var selComp = _components.FirstOrDefault(c => c.Id == _selectedId);
        ComponentListBox.SelectedIndex = selComp != null ? _components.IndexOf(selComp) : -1;
        _suppressSync = false;
    }

    // ==================== Property Editing ====================

    void SyncEditorsFromComponent()
    {
        var comp = GetSelectedComponent();
        if (comp == null) return;
        _suppressSync = true;

        ScaleSlider.Value = comp.Scale; ScaleBox.Text = comp.Scale.ToString("F2");
        PosXSlider.Value = comp.X; PosXBox.Text = comp.X.ToString("F0");
        PosYSlider.Value = comp.Y; PosYBox.Text = comp.Y.ToString("F0");
        PropForeground.Text = comp.ForegroundColor;
        PropBackground.Text = comp.BackgroundColor;
        PropTransparent.IsChecked = comp.TransparentBackground;
        PropFontFamily.Text = comp.FontFamily;
        SelectFontItem(comp.FontFamily);
        FontSizeSlider.Value = comp.FontSize; FontSizeBox.Text = comp.FontSize.ToString("F0");

        if (comp is ProgressBarComponent pb) { PropShowValueText.IsChecked = pb.ShowValueText; PropProgressTextColor.Text = pb.TextColor; BorderThicknessSlider.Value = pb.BorderThickness; BorderThicknessBox.Text = pb.BorderThickness.ToString("F0"); PropBorderColor.Text = pb.BorderColor; RoundnessSlider.Value = pb.Roundness; RoundnessBox.Text = pb.Roundness.ToString("F0"); }
        if (comp is CircularGaugeComponent cg) { SweepAngleSlider.Value = cg.SweepAngle; SweepAngleBox.Text = cg.SweepAngle.ToString("F0"); StartAngleSlider.Value = cg.StartAngle; StartAngleBox.Text = cg.StartAngle.ToString("F0"); RingThicknessSlider.Value = cg.RingThickness; RingThicknessBox.Text = cg.RingThickness.ToString("F0"); PropHideTrack.IsChecked = cg.HideTrack; PropTrackColor.Text = cg.TrackColor; PropNeedleEnabled.IsChecked = cg.NeedleEnabled; PropNeedleColor.Text = cg.NeedleColor; NeedleWidthSlider.Value = cg.NeedleWidth; NeedleWidthBox.Text = cg.NeedleWidth.ToString("F1"); PropShowCenterValue.IsChecked = cg.ShowCenterValue; PropGaugeTextColor.Text = cg.TextColor; }
        if (comp is DigitalDisplayComponent dd) { PropShowSuffix.IsChecked = dd.ShowSuffix; DecimalPlacesSlider.Value = dd.DecimalPlaces; DecimalPlacesBox.Text = dd.DecimalPlaces.ToString(); PropStrokeColor.Text = dd.StrokeColor; StrokeThicknessSlider.Value = dd.StrokeThickness; StrokeThicknessBox.Text = dd.StrokeThickness.ToString("F1"); }
        if (comp is SensorLabelComponent sl) { PropLabelShowPrefix.IsChecked = sl.ShowPrefix; PropLabelShowSuffix.IsChecked = sl.ShowSuffix; HierarchyLevelsSlider.Value = sl.HierarchyLevels; HierarchyLevelsBox.Text = sl.HierarchyLevels.ToString(); PropLabelStrokeColor.Text = sl.StrokeColor; LabelStrokeThicknessSlider.Value = sl.StrokeThickness; LabelStrokeThicknessBox.Text = sl.StrokeThickness.ToString("F1"); }
        if (comp is GridChartComponent gc) { DurationSlider.Value = gc.DurationSeconds; DurationBox.Text = gc.DurationSeconds.ToString(); GridXSlider.Value = gc.GridDensityX; GridXBox.Text = gc.GridDensityX.ToString(); GridYSlider.Value = gc.GridDensityY; GridYBox.Text = gc.GridDensityY.ToString(); LineWidthSlider.Value = gc.LineWidth; LineWidthBox.Text = gc.LineWidth.ToString("F1"); PropLineColor.Text = gc.LineColor; PropGridLineColor.Text = gc.GridLineColor; SmoothFactorSlider.Value = gc.SmoothFactor; SmoothFactorBox.Text = gc.SmoothFactor.ToString("F2"); PropShowFill.IsChecked = gc.ShowFill; FillOpacitySlider.Value = gc.FillOpacity; FillOpacityBox.Text = gc.FillOpacity.ToString("F2"); }

        _suppressSync = false;
        RefreshAllSwatches();
    }

    void RefreshAllSwatches()
    {
        foreach (var kv in _swatches)
            UpdateSwatchFill(kv.Value, kv.Key.Text);
    }

    void ApplyEditorToComponent()
    {
        var comp = GetSelectedComponent();
        if (comp == null) return;
        comp.Scale = ScaleSlider.Value;
        comp.X = PosXSlider.Value;
        comp.Y = PosYSlider.Value;
        comp.ForegroundColor = PropForeground.Text;
        comp.BackgroundColor = PropBackground.Text;
        comp.TransparentBackground = PropTransparent.IsChecked ?? true;
        comp.FontFamily = ((ComboBoxItem)PropFontFamily.SelectedItem).Content?.ToString() ?? "Consolas";
        comp.FontSize = FontSizeSlider.Value;

        if (comp is ProgressBarComponent pb) { pb.ShowValueText = PropShowValueText.IsChecked ?? true; pb.TextColor = PropProgressTextColor.Text; pb.BorderThickness = BorderThicknessSlider.Value; pb.BorderColor = PropBorderColor.Text; pb.Roundness = RoundnessSlider.Value; }
        if (comp is CircularGaugeComponent cg) { cg.SweepAngle = SweepAngleSlider.Value; cg.StartAngle = StartAngleSlider.Value; cg.RingThickness = RingThicknessSlider.Value; cg.HideTrack = PropHideTrack.IsChecked ?? false; cg.TrackColor = PropTrackColor.Text; cg.NeedleEnabled = PropNeedleEnabled.IsChecked ?? true; cg.NeedleColor = PropNeedleColor.Text; cg.NeedleWidth = NeedleWidthSlider.Value; cg.ShowCenterValue = PropShowCenterValue.IsChecked ?? true; cg.TextColor = PropGaugeTextColor.Text; }
        if (comp is DigitalDisplayComponent dd) { dd.ShowSuffix = PropShowSuffix.IsChecked ?? true; dd.DecimalPlaces = (int)DecimalPlacesSlider.Value; dd.StrokeColor = PropStrokeColor.Text; dd.StrokeThickness = StrokeThicknessSlider.Value; }
        if (comp is SensorLabelComponent sl) { sl.ShowPrefix = PropLabelShowPrefix.IsChecked ?? false; sl.ShowSuffix = PropLabelShowSuffix.IsChecked ?? true; sl.HierarchyLevels = (int)HierarchyLevelsSlider.Value; sl.StrokeColor = PropLabelStrokeColor.Text; sl.StrokeThickness = LabelStrokeThicknessSlider.Value; }
        if (comp is GridChartComponent gc) { gc.DurationSeconds = (int)DurationSlider.Value; gc.GridDensityX = (int)GridXSlider.Value; gc.GridDensityY = (int)GridYSlider.Value; gc.LineWidth = LineWidthSlider.Value; gc.LineColor = PropLineColor.Text; gc.GridLineColor = PropGridLineColor.Text; gc.SmoothFactor = SmoothFactorSlider.Value; gc.ShowFill = PropShowFill.IsChecked ?? false; gc.FillOpacity = FillOpacitySlider.Value; }

        BoundSensorLabel.Text = comp.BindingId != null ? $"Sensor: {comp.BindingId}" : "No sensor bound";
    }

    void RefreshSelectedControl()
    {
        var ctrl = GetSelectedControl();
        var comp = GetSelectedComponent();
        if (ctrl == null || comp == null) return;

        var (bw, bh) = GetBaseSize(comp.ComponentType);
        ctrl.Width = bw;
        ctrl.Height = bh;
        ctrl.RenderTransform = new ScaleTransform(comp.Scale, comp.Scale);
        ctrl.RenderTransformOrigin = new Point(0, 0);
        Canvas.SetLeft(ctrl, comp.X);
        Canvas.SetTop(ctrl, comp.Y);
        Canvas.SetZIndex(ctrl, comp.ZIndex);

        if (comp.BindingId == null && GetSimulatedSensor() is SensorValue sv)
            PushSensorValueToControl(ctrl, comp, sv);

        if (ctrl is ProgressBarControl pb) { pb.ApplyComponentData(); pb.RefreshValue(); }
        else if (ctrl is DigitalDisplayControl dd) dd.Refresh();
        else ctrl.InvalidateVisual();
    }

    void RefreshAllControls()
    {
        foreach (var (id, ctrl) in _controlMap)
        {
            var comp = _components.FirstOrDefault(c => c.Id == id);
            if (comp == null) continue;
            var (bw, bh) = GetBaseSize(comp.ComponentType);
            ctrl.Width = bw;
            ctrl.Height = bh;
            ctrl.RenderTransform = new ScaleTransform(comp.Scale, comp.Scale);
            ctrl.RenderTransformOrigin = new Point(0, 0);
            Canvas.SetLeft(ctrl, comp.X);
            Canvas.SetTop(ctrl, comp.Y);
            Canvas.SetZIndex(ctrl, comp.ZIndex);

            if (comp.BindingId == null && GetSimulatedSensor() is SensorValue sv)
                PushSensorValueToControl(ctrl, comp, sv);
        }
    }

    // ==================== Drag & Pan ====================

    void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _colorPopup.IsOpen = false;
        var pos = e.GetPosition(RenderCanvas);

        var hit = HitTestComponents(pos);
        if (hit != null)
        {
            SelectComponent(hit.Id);
            var ctrl = GetSelectedControl();
            var comp = GetSelectedComponent();
            if (ctrl != null && comp != null)
            {
                _isDragging = true;
                _dragStartMouse = pos;
                _dragStartX = comp.X;
                _dragStartY = comp.Y;
                RenderCanvas.CaptureMouse();
            }
        }
        else if (HitTestBgImage(pos))
        {
            SelectBackground();
        }
        else
        {
            _isPanning = true;
            _panStartMouse = e.GetPosition(this);
            _panStartScrollX = CanvasScroller.HorizontalOffset;
            _panStartScrollY = CanvasScroller.VerticalOffset;
            RenderCanvas.CaptureMouse();
        }
    }

    void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var ctrl = GetSelectedControl();
            var comp = GetSelectedComponent();
            if (ctrl == null || comp == null) return;

            var pos = e.GetPosition(RenderCanvas);
            var (bw, bh) = GetBaseSize(comp.ComponentType);

            double newX = _dragStartX + (pos.X - _dragStartMouse.X);
            double newY = _dragStartY + (pos.Y - _dragStartMouse.Y);
            newX = Math.Max(0, Math.Min(RenderCanvas.Width - bw * comp.Scale, newX));
            newY = Math.Max(0, Math.Min(RenderCanvas.Height - bh * comp.Scale, newY));

            comp.X = newX;
            comp.Y = newY;
            Canvas.SetLeft(ctrl, newX);
            Canvas.SetTop(ctrl, newY);

            _suppressSync = true;
            PosXSlider.Value = newX; PosXBox.Text = newX.ToString("F0");
            PosYSlider.Value = newY; PosYBox.Text = newY.ToString("F0");
            _suppressSync = false;
        }
        else if (_isPanning)
        {
            var current = e.GetPosition(this);
            double dx = _panStartMouse.X - current.X;
            double dy = _panStartMouse.Y - current.Y;
            CanvasScroller.ScrollToHorizontalOffset(_panStartScrollX + dx);
            CanvasScroller.ScrollToVerticalOffset(_panStartScrollY + dy);
        }
    }

    void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            RenderCanvas.ReleaseMouseCapture();
        }
        else if (_isPanning)
        {
            _isPanning = false;
            RenderCanvas.ReleaseMouseCapture();
            var current = e.GetPosition(this);
            double dist = Math.Abs(current.X - _panStartMouse.X) + Math.Abs(current.Y - _panStartMouse.Y);
            if (dist < 3)
                SelectComponent(null);
        }
    }

    void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            double delta = e.Delta > 0 ? 0.1 : -0.1;
            double newVal = Math.Max(CanvasZoomSlider.Minimum, Math.Min(CanvasZoomSlider.Maximum, CanvasZoomSlider.Value + delta));
            CanvasZoomSlider.Value = newVal;
            e.Handled = true;
        }
    }

    void Canvas_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _selectedId.HasValue)
        {
            RemoveComponent(_selectedId.Value);
        }
    }

    bool HitTestBgImage(Point pos)
    {
        if (BgImage.Source == null || BgImage.Visibility != Visibility.Visible)
            return false;

        double left = Canvas.GetLeft(BgImage);
        double top = Canvas.GetTop(BgImage);
        double scale = EffectiveScale;

        if (BgImage.Source is BitmapSource bmp)
        {
            double w = bmp.Width * scale;
            double h = bmp.Height * scale;
            return pos.X >= left && pos.X <= left + w && pos.Y >= top && pos.Y <= top + h;
        }
        return false;
    }

    Component? HitTestComponents(Point pos)
    {
        Component? best = null;
        int bestZ = int.MinValue;
        foreach (var comp in _components)
        {
            if (!_controlMap.TryGetValue(comp.Id, out var ctrl)) continue;
            var (bw, bh) = GetBaseSize(comp.ComponentType);
            double sw = bw * comp.Scale;
            double sh = bh * comp.Scale;
            double left = comp.X;
            double top = comp.Y;
            if (pos.X >= left && pos.X <= left + sw && pos.Y >= top && pos.Y <= top + sh)
            {
                if (comp.ZIndex > bestZ) { best = comp; bestZ = comp.ZIndex; }
            }
        }
        return best;
    }

    // ==================== Sensor Data ====================

    SensorValue GetSimulatedSensor()
    {
        double v = ValueSlider.Value;
        return new SensorValue { BindingId = "/sim/component/value", CurrentValue = (float)v, DisplayText = $"{v:F0}%", Unit = "%", ValueType = SensorValueType.Continuous, UpperBound = 100, LowerBound = 0 };
    }

    void PushSimulatedToUnbound()
    {
        var sv = GetSimulatedSensor();
        foreach (var (id, ctrl) in _controlMap)
        {
            var comp = _components.FirstOrDefault(c => c.Id == id);
            if (comp != null && comp.BindingId == null)
                PushSensorValueToControl(ctrl, comp, sv);
        }
    }

    void PushLiveSensorData()
    {
        bool anyBound = false;
        foreach (var comp in _components.Where(c => c.BindingId != null))
        {
            var sv = HardwareService.Instance.GetSensor(comp.BindingId!);
            if (sv == null) continue;
            anyBound = true;
            if (_controlMap.TryGetValue(comp.Id, out var ctrl))
                PushSensorValueToControl(ctrl, comp, sv);
        }
        if (!anyBound) PushSimulatedToUnbound();
    }

    static void PushSensorValueToControl(UserControl ctrl, Component comp, SensorValue sv)
    {
        if (ctrl is ProgressBarControl pb) { pb.SensorValue = sv; pb.RefreshValue(); }
        else if (ctrl is CircularGaugeControl cg) { cg.SensorValue = sv; cg.InvalidateVisual(); }
        else if (ctrl is DigitalDisplayControl dd) { dd.SensorValue = sv; dd.Refresh(); }
        else if (ctrl is GridChartControl gc) { gc.SensorValue = sv; gc.InvalidateVisual(); }
        else if (ctrl is SensorLabelControl sl) { sl.SensorValue = sv; sl.Refresh(); }
    }

    void AppendAllChartHistories()
    {
        foreach (var comp in _components)
        {
            if (comp is not GridChartComponent gc) continue;
            float v = gc.BindingId != null && _controlMap.TryGetValue(gc.Id, out var ctrl) && ctrl is GridChartControl gcc && gcc.SensorValue != null
                ? gcc.SensorValue.CurrentValue : (float)ValueSlider.Value;
            gc.HistoryValues.Add((DateTime.Now, v));
            while (gc.HistoryValues.Count > 0 && (DateTime.Now - gc.HistoryValues[0].Time).TotalSeconds > gc.DurationSeconds * 2)
                gc.HistoryValues.RemoveAt(0);
            if (_controlMap.TryGetValue(gc.Id, out var gcCtrl))
                gcCtrl.InvalidateVisual();
        }
    }

    // ==================== Top Bar Event Handlers ====================

    void AddGaugeBtn_Click(object s, RoutedEventArgs e) => AddComponent(ComponentType.CircularGauge);
    void AddProgressBtn_Click(object s, RoutedEventArgs e) => AddComponent(ComponentType.ProgressBar);
    void AddDisplayBtn_Click(object s, RoutedEventArgs e) => AddComponent(ComponentType.DigitalDisplay);
    void AddChartBtn_Click(object s, RoutedEventArgs e) => AddComponent(ComponentType.GridChart);
    void AddLabelBtn_Click(object s, RoutedEventArgs e) => AddComponent(ComponentType.SensorLabel);

    void DeleteBtn_Click(object s, RoutedEventArgs e)
    {
        if (_selectedId.HasValue) RemoveComponent(_selectedId.Value);
    }

    void ValueSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready) return;
        ValueLabel.Text = $"{e.NewValue:F0}%";
        PushSimulatedToUnbound();
    }

    void CanvasZoom_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready) return;
        CanvasZoomLabel.Text = $"{e.NewValue * 100:F0}%";
        ApplyCanvasZoom();
    }

    void ApplyCanvasZoom()
    {
        var scale = CanvasZoomSlider.Value;
        RenderCanvas.LayoutTransform = new ScaleTransform(scale, scale);
    }

    void CanvasSize_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        ApplyCanvasSize();
    }

    void CanvasSizeBtn_Click(object s, RoutedEventArgs e) => ApplyCanvasSize();

    void ApplyCanvasSize()
    {
        if (double.TryParse(CanvasWidthBox.Text, out double w) && w >= 100 && w <= 8000 &&
            double.TryParse(CanvasHeightBox.Text, out double h) && h >= 100 && h <= 8000)
        {
            RenderCanvas.Width = w;
            RenderCanvas.Height = h;
            PosXSlider.Maximum = w;
            PosYSlider.Maximum = h;
        }
        else
        {
            CanvasWidthBox.Text = RenderCanvas.Width.ToString("F0");
            CanvasHeightBox.Text = RenderCanvas.Height.ToString("F0");
        }
    }

    void SetupHwBtn_Click(object s, RoutedEventArgs e)
    {
        new HardwareSelectDialog().ShowDialog();
        SensorTree.PopulateTree();
        _sensorTimer.Start();
        _chartTimer.Start();
    }

    // ==================== Left Panel Event Handlers ====================

    void ComponentList_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (!_ready || _suppressSync) return;
        if (ComponentListBox.SelectedItem is Component comp && comp.Id != _selectedId)
            SelectComponent(comp.Id);
        else if (ComponentListBox.SelectedItem == null)
            SelectComponent(null);
    }

    void OnSensorTreeSelected(string? id)
    {
        if (id == null) return;
        var comp = GetSelectedComponent();
        if (comp == null) return;
        BindSensorToComponent(comp, id);
    }

    void BindSensorBtn_Click(object s, RoutedEventArgs e)
    {
        var comp = GetSelectedComponent();
        if (comp == null) return;
        var bindingId = SensorTree.SelectedBindingId;
        if (bindingId == null) return;
        BindSensorToComponent(comp, bindingId);
    }

    void BindSensorToComponent(Component comp, string bindingId)
    {
        comp.BindingId = bindingId;
        BoundSensorLabel.Text = $"Sensor: {bindingId}";
        var sv = HardwareService.Instance.GetSensor(bindingId);
        if (sv != null && _controlMap.TryGetValue(comp.Id, out var ctrl))
            PushSensorValueToControl(ctrl, comp, sv);
        _sensorTimer.Start();
    }

    // ==================== Property Event Handlers ====================

    void SliderVC(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready || _suppressSync) return;
        var sl = (Slider)s;
        var box = (TextBox)sl.Tag;
        box.Text = sl.Value.ToString(sl.IsSnapToTickEnabled ? "F0" : "F2");
        ApplyEditorToComponent();
        RefreshSelectedControl();
    }

    void BoxKD(object s, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var box = (TextBox)s;
        var sl = (Slider)box.Tag;
        if (double.TryParse(box.Text, out double v))
        {
            v = Math.Max(sl.Minimum, Math.Min(sl.Maximum, v));
            sl.Value = v;
        }
        else box.Text = sl.Value.ToString("F2");
    }

    void CheckBox_Changed(object s, RoutedEventArgs e)
    {
        if (!_ready || _suppressSync) return;
        ApplyEditorToComponent();
        RefreshSelectedControl();
    }

    void PropFontFamily_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (!_ready || _suppressSync) return;
        ApplyEditorToComponent();
        RefreshSelectedControl();
    }

    void SelectFontItem(string family)
    {
        foreach (ComboBoxItem item in PropFontFamily.Items)
            if (item.Content?.ToString() == family) { item.IsSelected = true; return; }
    }

    // ==================== Save / Load ====================

    // ==================== Save / Load ====================

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

    void UpdateTitle()
    {
        string name = _currentFilePath != null ? Path.GetFileNameWithoutExtension(_currentFilePath) : "未保存";
        Title = $"主题编辑器 - {name}";
        ThemeNameLabel.Text = name;
    }

    DashboardConfig BuildConfig()
    {
        return new DashboardConfig
        {
            Version = "1.0",
            ThemeName = _currentFilePath != null ? Path.GetFileNameWithoutExtension(_currentFilePath) : "Untitled",
            CanvasWidth = (double)RenderCanvas.Width,
            CanvasHeight = (double)RenderCanvas.Height,
            BackgroundColor = PropBgColor.Text,
            BackgroundImagePath = PropBgImagePath.Text,
            BackgroundImageScale = EffectiveScale,
            BackgroundImageOffsetX = BgOffsetXSlider.Value,
            BackgroundImageOffsetY = BgOffsetYSlider.Value,
            Components = _components.ToList()
        };
    }

    void SaveThemeBtn_Click(object s, RoutedEventArgs e)
    {
        if (_currentFilePath != null)
        {
            try
            {
                var config = BuildConfig();
                ConfigService.Save(config, _currentFilePath);
                UpdateTitle();
                MessageBox.Show("保存完成。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            SaveAsBtn_Click(s, e);
        }
    }

    void SaveAsBtn_Click(object s, RoutedEventArgs e)
    {
        string? name = ShowInputDialog("输入主题名称：", "另存为");
        if (string.IsNullOrWhiteSpace(name)) return;

        string path = Path.Combine(ConfigService.ThemesDirectory, name + ".json");
        if (File.Exists(path))
        {
            var result = MessageBox.Show($"主题「{name}」已存在，是否覆盖？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
        }

        try
        {
            var config = BuildConfig();
            config.ThemeName = name;
            ConfigService.Save(config, path);
            _currentFilePath = path;
            UpdateTitle();
            MessageBox.Show("保存完成。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void LoadThemeBtn_Click(object s, RoutedEventArgs e)
    {
        var themes = ConfigService.ListThemes();
        if (themes.Count == 0)
        {
            MessageBox.Show("没有已保存的主题。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new System.Windows.Forms.Form
        {
            Text = "加载主题",
            Width = 360,
            Height = 340,
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
            ShowInTaskbar = false
        };

        var listBox = new System.Windows.Forms.ListBox
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            Font = new System.Drawing.Font("Microsoft YaHei", 11)
        };
        listBox.Items.AddRange(themes.ToArray<object>());
        dialog.Controls.Add(listBox);

        var btnPanel = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Bottom, Height = 44 };
        var okBtn = new System.Windows.Forms.Button { Text = "加载", Width = 80, Height = 28, Left = 140, Top = 8 };
        var cancelBtn = new System.Windows.Forms.Button { Text = "取消", Width = 80, Height = 28, Left = 228, Top = 8 };
        okBtn.Click += (_, _) => { dialog.DialogResult = System.Windows.Forms.DialogResult.OK; dialog.Close(); };
        cancelBtn.Click += (_, _) => { dialog.DialogResult = System.Windows.Forms.DialogResult.Cancel; dialog.Close(); };
        listBox.DoubleClick += (_, _) => { dialog.DialogResult = System.Windows.Forms.DialogResult.OK; dialog.Close(); };
        btnPanel.Controls.Add(okBtn);
        btnPanel.Controls.Add(cancelBtn);
        dialog.Controls.Add(btnPanel);

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        if (listBox.SelectedItem is not string selectedName) return;

        string path = Path.Combine(ConfigService.ThemesDirectory, selectedName + ".json");
        if (!File.Exists(path)) return;

        try
        {
            LoadConfig(path);
            _currentFilePath = path;
            UpdateTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void LoadConfig(string path)
    {
        var config = ConfigService.Load(path);
        RenderCanvas.Width = config.CanvasWidth;
        RenderCanvas.Height = config.CanvasHeight;
        CanvasWidthBox.Text = config.CanvasWidth.ToString("F0");
        CanvasHeightBox.Text = config.CanvasHeight.ToString("F0");
        PosXSlider.Maximum = config.CanvasWidth;
        PosYSlider.Maximum = config.CanvasHeight;
        CanvasZoomSlider.Value = 1.0;
        ApplyCanvasZoom();

        foreach (var ctrl in _controlMap.Values)
            RenderCanvas.Children.Remove(ctrl);
        _controlMap.Clear();
        _components.Clear();
        _selectedId = null;

        foreach (var comp in config.Components)
        {
            _components.Add(comp);
            var ctrl = CreateControlForComponent(comp);
            _controlMap[comp.Id] = ctrl;
            RenderCanvas.Children.Add(ctrl);
        }

        PushSimulatedToUnbound();
        SelectComponent(_components.FirstOrDefault()?.Id);
        LoadBackgroundFromConfig(config);
        _sensorTimer.Start();
        _chartTimer.Start();
    }

    void LoadBackgroundFromConfig(DashboardConfig config)
    {
        PropBgColor.Text = config.BackgroundColor;
        BgColorSwatch.Fill = new SolidColorBrush(ParseColor(config.BackgroundColor));
        RenderCanvas.Background = new SolidColorBrush(ParseColor(config.BackgroundColor));
        PropBgImagePath.Text = config.BackgroundImagePath;
        SetEffectiveScale(config.BackgroundImageScale);
        BgOffsetXSlider.Value = config.BackgroundImageOffsetX;
        BgOffsetXBox.Text = config.BackgroundImageOffsetX.ToString("F0");
        BgOffsetYSlider.Value = config.BackgroundImageOffsetY;
        BgOffsetYBox.Text = config.BackgroundImageOffsetY.ToString("F0");
        ApplyBackgroundImage();
    }

    void ApplyBackgroundImage()
    {
        string path = PropBgImagePath.Text;
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
                UpdateBgImageTransform();
                return;
            }
        }
        BgImage.Source = null;
        BgImage.Visibility = Visibility.Collapsed;
    }

    void UpdateBgImageTransform()
    {
        double scale = EffectiveScale;
        double ox = BgOffsetXSlider.Value;
        double oy = BgOffsetYSlider.Value;
        BgImage.RenderTransform = new ScaleTransform(scale, scale);
        BgImage.RenderTransformOrigin = new Point(0, 0);
        Canvas.SetLeft(BgImage, ox);
        Canvas.SetTop(BgImage, oy);
    }

    // ==================== Background Image ====================

    void BgImageBtn_Click(object s, RoutedEventArgs e)
    {
        SelectBackground();
    }

    void BgImage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        SelectBackground();
        e.Handled = true;
    }

    void SelectBackground()
    {
        _backgroundSelected = true;
        _selectedId = null;
        UpdateSelectionUI();
        PropertyPanelRoot.BringIntoView();
    }

    void BrowseBgImageBtn_Click(object s, RoutedEventArgs e)
    {
        string themeImgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "themeImg");
        Directory.CreateDirectory(themeImgDir);
        var dlg = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Title = "选择背景图片",
            InitialDirectory = themeImgDir
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        string selectedPath = dlg.FileName;
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (selectedPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
        {
            selectedPath = selectedPath.Substring(baseDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        PropBgImagePath.Text = selectedPath;
        ApplyBackgroundImage();
    }

    void SaveBgToDirBtn_Click(object s, RoutedEventArgs e)
    {
        string src = PropBgImagePath.Text;
        if (string.IsNullOrEmpty(src) || !File.Exists(src)) return;

        string fullSrc = Path.IsPathRooted(src) ? src : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, src);
        string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (fullSrc.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            return;

        string themeImgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "themeImg");
        Directory.CreateDirectory(themeImgDir);

        string ext = Path.GetExtension(src);
        string destName = $"bg_{DateTime.Now:yyyyMMddHHmmss}{ext}";
        string dest = Path.Combine(themeImgDir, destName);

        try
        {
            File.Copy(src, dest, overwrite: true);
            string relativePath = Path.Combine("themeImg", destName);
            PropBgImagePath.Text = relativePath;
            ApplyBackgroundImage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void ClearBgImageBtn_Click(object s, RoutedEventArgs e)
    {
        PropBgImagePath.Text = "";
        ApplyBackgroundImage();
    }

    void BgColorSwatch_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _activeColorBox = PropBgColor;
        _colorPopup.SetCurrentColor(PropBgColor.Text);
        _colorPopup.PlacementTarget = BgColorSwatch;
        _colorPopup.IsOpen = true;
    }

    double EffectiveScale => BgScaleSlider.Value * BgScaleSlider.Value;

    void SetEffectiveScale(double scale)
    {
        double sliderVal = Math.Sqrt(Math.Max(0.01, scale));
        sliderVal = Math.Clamp(sliderVal, BgScaleSlider.Minimum, BgScaleSlider.Maximum);
        BgScaleSlider.Value = sliderVal;
        BgScaleBox.Text = scale.ToString("F2");
    }

    void BgScaleSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready || _suppressSync) return;
        double effective = EffectiveScale;
        BgScaleBox.Text = effective.ToString("F2");
        UpdateBgImageTransform();
    }

    void BgScaleBox_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (double.TryParse(BgScaleBox.Text, out double v))
        {
            double sliderVal = Math.Sqrt(Math.Max(0.01, v));
            sliderVal = Math.Clamp(sliderVal, BgScaleSlider.Minimum, BgScaleSlider.Maximum);
            BgScaleSlider.Value = sliderVal;
        }
        else BgScaleBox.Text = EffectiveScale.ToString("F2");
    }

    void BgScaleReset_Click(object s, RoutedEventArgs e)
    {
        SetEffectiveScale(1.0);
        BgOffsetXSlider.Value = 0;
        BgOffsetYSlider.Value = 0;
    }

    void BgScaleFitWidth_Click(object s, RoutedEventArgs e)
    {
        if (BgImage.Source is BitmapSource bmp)
        {
            double scale = RenderCanvas.Width / bmp.Width;
            SetEffectiveScale(scale);
            BgOffsetXSlider.Value = 0;
        }
    }

    void BgScaleFitHeight_Click(object s, RoutedEventArgs e)
    {
        if (BgImage.Source is BitmapSource bmp)
        {
            double scale = RenderCanvas.Height / bmp.Height;
            SetEffectiveScale(scale);
            BgOffsetYSlider.Value = 0;
        }
    }

    void BgCenterH_Click(object s, RoutedEventArgs e)
    {
        if (BgImage.Source is BitmapSource bmp)
        {
            double offset = (RenderCanvas.Width - bmp.Width * EffectiveScale) / 2;
            BgOffsetXSlider.Value = Math.Round(offset, 0);
        }
    }

    void BgCenterV_Click(object s, RoutedEventArgs e)
    {
        if (BgImage.Source is BitmapSource bmp)
        {
            double offset = (RenderCanvas.Height - bmp.Height * EffectiveScale) / 2;
            BgOffsetYSlider.Value = Math.Round(offset, 0);
        }
    }

    void BgOffsetXSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready || _suppressSync) return;
        BgOffsetXBox.Text = BgOffsetXSlider.Value.ToString("F0");
        Canvas.SetLeft(BgImage, BgOffsetXSlider.Value);
    }

    void BgOffsetXBox_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (double.TryParse(BgOffsetXBox.Text, out double v))
        {
            v = Math.Max(BgOffsetXSlider.Minimum, Math.Min(BgOffsetXSlider.Maximum, v));
            BgOffsetXSlider.Value = v;
        }
        else BgOffsetXBox.Text = BgOffsetXSlider.Value.ToString("F0");
    }

    void BgOffsetYSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready || _suppressSync) return;
        BgOffsetYBox.Text = BgOffsetYSlider.Value.ToString("F0");
        Canvas.SetTop(BgImage, BgOffsetYSlider.Value);
    }

    void BgOffsetYBox_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (double.TryParse(BgOffsetYBox.Text, out double v))
        {
            v = Math.Max(BgOffsetYSlider.Minimum, Math.Min(BgOffsetYSlider.Maximum, v));
            BgOffsetYSlider.Value = v;
        }
        else BgOffsetYBox.Text = BgOffsetYSlider.Value.ToString("F0");
    }

    Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Colors.Black; }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _sensorTimer.Stop();
        _chartTimer.Stop();
        base.OnClosing(e);
    }
}
