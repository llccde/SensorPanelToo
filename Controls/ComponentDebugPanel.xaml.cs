using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SensorPanelToo.Models;
using SensorPanelToo.Services;

namespace SensorPanelToo.Controls;

public partial class ComponentDebugPanel : UserControl
{
    private ComponentType _currentType = ComponentType.CircularGauge;
    private string? _bindingId;
    private readonly DispatcherTimer _chartTimer = new();
    private readonly DispatcherTimer _realDataTimer = new();
    private bool _ready;
    private bool _suppressSync;
    private TextBox? _activeColorBox;
    private readonly ColorPickerPopup _colorPopup = new();
    private readonly Dictionary<TextBox, System.Windows.Shapes.Rectangle> _swatches = new();

    public ComponentDebugPanel()
    {
        InitializeComponent();
        InitChartTimer();
        InitComponentModels();
        InitColorTextBoxes();
        WireAllPairs();
        _ready = true;
        ApplyComponentType(ComponentType.CircularGauge);
        ApplyAndRefresh();
    }

    void InitChartTimer()
    {
        _chartTimer.Interval = TimeSpan.FromMilliseconds(500);
        _chartTimer.Tick += (_, _) =>
        {
            if (_currentType == ComponentType.GridChart) { AppendChartHistory(); GridChartCtrl.InvalidateVisual(); }
        };
        _realDataTimer.Interval = TimeSpan.FromMilliseconds(500);
        _realDataTimer.Tick += (_, _) =>
        {
            if (_bindingId == null) return;
            var sv = HardwareService.Instance.GetSensor(_bindingId);
            if (sv != null) { PushSensorValue(sv); if (_currentType == ComponentType.GridChart) AppendChartHistory(); RealSensorLabel.Text = sv.DisplayText; }
        };
    }

    void InitComponentModels()
    {
        CircularGaugeCtrl.ComponentData = new CircularGaugeComponent { ForegroundColor = "#FF6644", RingThickness = 14, ShowCenterValue = true };
        ProgressBarCtrl.ComponentData = new ProgressBarComponent { ForegroundColor = "#00AA44", BackgroundColor = "#E0E0E0", ShowValueText = true, TransparentBackground = false };
        DigitalDisplayCtrl.ComponentData = new DigitalDisplayComponent { ForegroundColor = "#0066CC", StrokeColor = "#CCCCCC", StrokeThickness = 1, ShowSuffix = true, DecimalPlaces = 1, FontWeight = FontWeightOption.Bold, FontSize = 36 };
        GridChartCtrl.ComponentData = new GridChartComponent { DurationSeconds = 30, GridDensityX = 5, GridDensityY = 4, GridLineColor = "#E0E0E0", LineColor = "#0066CC", LineWidth = 2, SmoothFactor = 0.3 };
    }

    void InitColorTextBoxes()
    {
        _colorPopup.ColorChanged += hex =>
        {
            if (_activeColorBox != null) { _activeColorBox.Text = hex; UpdateSwatchColor(_activeColorBox); ApplyAndRefresh(); }
        };
        _colorPopup.ColorSelected += hex =>
        {
            if (_activeColorBox != null) { _activeColorBox.Text = hex; UpdateSwatchColor(_activeColorBox); ApplyAndRefresh(); }
        };
        _colorPopup.ColorCancelled += hex =>
        {
            if (_activeColorBox != null) { _activeColorBox.Text = hex; UpdateSwatchColor(_activeColorBox); ApplyAndRefresh(); }
        };
        foreach (var b in new[] { PropForeground, PropBackground, PropProgressTextColor, PropGaugeTextColor, PropBorderColor, PropNeedleColor, PropStrokeColor, PropLineColor, PropGridLineColor })
        {
            b.GotFocus += (_, _) => { _activeColorBox = b; _colorPopup.SetCurrentColor(b.Text); _colorPopup.PlacementTarget = b; _colorPopup.IsOpen = true; };
            AttachSwatch(b);
        }
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
        WirePair(FontSizeSlider, FontSizeBox);
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
        WirePair(BorderThicknessSlider, BorderThicknessBox);
        WirePair(RoundnessSlider, RoundnessBox);
    }

    static void WirePair(Slider sl, TextBox box) { sl.Tag = box; box.Tag = sl; }

    void ApplyComponentType(ComponentType type)
    {
        _currentType = type;
        ProgressBarCtrl.Visibility = type == ComponentType.ProgressBar ? Visibility.Visible : Visibility.Collapsed;
        CircularGaugeCtrl.Visibility = type == ComponentType.CircularGauge ? Visibility.Visible : Visibility.Collapsed;
        DigitalDisplayCtrl.Visibility = type == ComponentType.DigitalDisplay ? Visibility.Visible : Visibility.Collapsed;
        GridChartCtrl.Visibility = type == ComponentType.GridChart ? Visibility.Visible : Visibility.Collapsed;
        PanelProgress.Visibility = type == ComponentType.ProgressBar ? Visibility.Visible : Visibility.Collapsed;
        PanelGauge.Visibility = type == ComponentType.CircularGauge ? Visibility.Visible : Visibility.Collapsed;
        PanelDisplay.Visibility = type == ComponentType.DigitalDisplay ? Visibility.Visible : Visibility.Collapsed;
        PanelChart.Visibility = type == ComponentType.GridChart ? Visibility.Visible : Visibility.Collapsed;
        _chartTimer.IsEnabled = type == ComponentType.GridChart;
        if (GetCurrentComponent() is Component comp) SyncEditorsFromComponent(comp);
    }

    Component? GetCurrentComponent() => _currentType switch
    {
        ComponentType.ProgressBar => ProgressBarCtrl.ComponentData,
        ComponentType.CircularGauge => CircularGaugeCtrl.ComponentData,
        ComponentType.DigitalDisplay => DigitalDisplayCtrl.ComponentData,
        ComponentType.GridChart => GridChartCtrl.ComponentData,
        _ => null
    };

    FrameworkElement? GetCurrentControl() => _currentType switch
    {
        ComponentType.ProgressBar => ProgressBarCtrl,
        ComponentType.CircularGauge => CircularGaugeCtrl,
        ComponentType.DigitalDisplay => DigitalDisplayCtrl,
        ComponentType.GridChart => GridChartCtrl,
        _ => null
    };

    (double w, double h) GetBaseSize() => _currentType switch
    {
        ComponentType.ProgressBar => (300, 40),
        ComponentType.CircularGauge => (200, 200),
        ComponentType.DigitalDisplay => (250, 80),
        ComponentType.GridChart => (400, 200),
        _ => (200, 200)
    };

    void SyncEditorsFromComponent(Component comp)
    {
        _suppressSync = true;
        ScaleSlider.Value = comp.Scale; ScaleBox.Text = comp.Scale.ToString("F2");
        PropForeground.Text = comp.ForegroundColor;
        PropBackground.Text = comp.BackgroundColor;
        PropTransparent.IsChecked = comp.TransparentBackground;
        PropFontFamily.Text = comp.FontFamily;
        SelectFontItem(comp.FontFamily);
        FontSizeSlider.Value = comp.FontSize; FontSizeBox.Text = comp.FontSize.ToString("F0");
        if (comp is ProgressBarComponent pb) { PropShowValueText.IsChecked = pb.ShowValueText; PropProgressTextColor.Text = pb.TextColor; BorderThicknessSlider.Value = pb.BorderThickness; BorderThicknessBox.Text = pb.BorderThickness.ToString("F0"); PropBorderColor.Text = pb.BorderColor; RoundnessSlider.Value = pb.Roundness; RoundnessBox.Text = pb.Roundness.ToString("F0"); }
        if (comp is CircularGaugeComponent cg) { SweepAngleSlider.Value = cg.SweepAngle; SweepAngleBox.Text = cg.SweepAngle.ToString("F0"); StartAngleSlider.Value = cg.StartAngle; StartAngleBox.Text = cg.StartAngle.ToString("F0"); RingThicknessSlider.Value = cg.RingThickness; RingThicknessBox.Text = cg.RingThickness.ToString("F0"); PropNeedleEnabled.IsChecked = cg.NeedleEnabled; PropNeedleColor.Text = cg.NeedleColor; NeedleWidthSlider.Value = cg.NeedleWidth; NeedleWidthBox.Text = cg.NeedleWidth.ToString("F1"); PropShowCenterValue.IsChecked = cg.ShowCenterValue; PropGaugeTextColor.Text = cg.TextColor; }
        if (comp is DigitalDisplayComponent dd) { PropShowSuffix.IsChecked = dd.ShowSuffix; DecimalPlacesSlider.Value = dd.DecimalPlaces; DecimalPlacesBox.Text = dd.DecimalPlaces.ToString(); PropStrokeColor.Text = dd.StrokeColor; StrokeThicknessSlider.Value = dd.StrokeThickness; StrokeThicknessBox.Text = dd.StrokeThickness.ToString("F1"); }
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
        if (GetCurrentComponent() is not Component comp) return;
        comp.Scale = ScaleSlider.Value;
        comp.ForegroundColor = PropForeground.Text;
        comp.BackgroundColor = PropBackground.Text;
        comp.TransparentBackground = PropTransparent.IsChecked ?? true;
        comp.FontFamily = ((ComboBoxItem)PropFontFamily.SelectedItem).Content?.ToString() ?? "Consolas";
        comp.FontSize = FontSizeSlider.Value;
        if (comp is ProgressBarComponent pb) { pb.ShowValueText = PropShowValueText.IsChecked ?? true; pb.TextColor = PropProgressTextColor.Text; pb.BorderThickness = BorderThicknessSlider.Value; pb.BorderColor = PropBorderColor.Text; pb.Roundness = RoundnessSlider.Value; }
        if (comp is CircularGaugeComponent cg) { cg.SweepAngle = SweepAngleSlider.Value; cg.StartAngle = StartAngleSlider.Value; cg.RingThickness = RingThicknessSlider.Value; cg.NeedleEnabled = PropNeedleEnabled.IsChecked ?? true; cg.NeedleColor = PropNeedleColor.Text; cg.NeedleWidth = NeedleWidthSlider.Value; cg.ShowCenterValue = PropShowCenterValue.IsChecked ?? true; cg.TextColor = PropGaugeTextColor.Text; }
        if (comp is DigitalDisplayComponent dd) { dd.ShowSuffix = PropShowSuffix.IsChecked ?? true; dd.DecimalPlaces = (int)DecimalPlacesSlider.Value; dd.StrokeColor = PropStrokeColor.Text; dd.StrokeThickness = StrokeThicknessSlider.Value; }
        if (comp is GridChartComponent gc) { gc.DurationSeconds = (int)DurationSlider.Value; gc.GridDensityX = (int)GridXSlider.Value; gc.GridDensityY = (int)GridYSlider.Value; gc.LineWidth = LineWidthSlider.Value; gc.LineColor = PropLineColor.Text; gc.GridLineColor = PropGridLineColor.Text; gc.SmoothFactor = SmoothFactorSlider.Value; gc.ShowFill = PropShowFill.IsChecked ?? false; gc.FillOpacity = FillOpacitySlider.Value; }
    }

    SensorValue MakeSimulatedSensor()
    {
        double v = ValueSlider.Value;
        return new SensorValue { BindingId = "/sim/component/value", CurrentValue = (float)v, DisplayText = $"{v:F0}%", Unit = "%", ValueType = SensorValueType.Continuous, UpperBound = 100, LowerBound = 0 };
    }

    void PushSensorValue(SensorValue sv) { ProgressBarCtrl.SensorValue = sv; CircularGaugeCtrl.SensorValue = sv; DigitalDisplayCtrl.SensorValue = sv; GridChartCtrl.SensorValue = sv; }

    void RefreshRender()
    {
        if (GetCurrentControl() is not FrameworkElement ctrl || GetCurrentComponent() is not Component comp) return;
        if (_bindingId == null) PushSensorValue(MakeSimulatedSensor());
        var (bw, bh) = GetBaseSize();
        ctrl.Width = bw; ctrl.Height = bh;
        ctrl.RenderTransform = new ScaleTransform(comp.Scale, comp.Scale);
        ctrl.RenderTransformOrigin = new Point(0, 0);
        double sw = bw * comp.Scale, sh = bh * comp.Scale;
        Canvas.SetLeft(ctrl, (480 - sw) / 2);
        Canvas.SetTop(ctrl, (480 - sh) / 2);
        ctrl.InvalidateVisual();
    }

    void ApplyAndRefresh() { ApplyEditorToComponent(); RefreshRender(); SyncCurrentControl(); }

    void SyncCurrentControl()
    {
        var ctrl = GetCurrentControl();
        if (ctrl is ProgressBarControl pb) { pb.ApplyComponentData(); pb.RefreshValue(); }
        else if (ctrl is DigitalDisplayControl dd) dd.Refresh();
        else if (ctrl is CircularGaugeControl cg) cg.InvalidateVisual();
        else if (ctrl is GridChartControl gc) gc.InvalidateVisual();
    }

    void AppendChartHistory()
    {
        if (GridChartCtrl.ComponentData is not GridChartComponent gc) return;
        float v = _bindingId != null && GridChartCtrl.SensorValue != null ? GridChartCtrl.SensorValue.CurrentValue : (float)ValueSlider.Value;
        gc.HistoryValues.Add((DateTime.Now, v));
        while (gc.HistoryValues.Count > 0 && (DateTime.Now - gc.HistoryValues[0].Time).TotalSeconds > gc.DurationSeconds * 2) gc.HistoryValues.RemoveAt(0);
    }

    void SliderVC(object s, RoutedPropertyChangedEventArgs<double> e) { if (!_ready || _suppressSync) return; var sl = (Slider)s; var box = (TextBox)sl.Tag; box.Text = sl.Value.ToString("F2"); ApplyAndRefresh(); }
    void BoxKD(object s, KeyEventArgs e) { if (e.Key != Key.Enter) return; var box = (TextBox)s; var sl = (Slider)box.Tag; if (double.TryParse(box.Text, out double v)) { v = Math.Max(sl.Minimum, Math.Min(sl.Maximum, v)); sl.Value = v; } else box.Text = sl.Value.ToString("F2"); }

    void ComponentTypeCombo_SelectionChanged(object s, SelectionChangedEventArgs e) { if (!_ready) return; if (ComponentTypeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag && Enum.TryParse<ComponentType>(tag, out var t)) { ApplyComponentType(t); RefreshRender(); } }
    void ValueSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e) { if (!_ready) return; ValueLabel.Text = $"{e.NewValue:F0}%"; if (_bindingId == null) RefreshRender(); }
    void CheckBox_Changed(object s, RoutedEventArgs e) { if (!_ready || _suppressSync) return; ApplyAndRefresh(); }

    void SetupHwBtn_Click(object s, RoutedEventArgs e) => new Views.HardwareSelectDialog().ShowDialog();
    void SelectSensorBtn_Click(object s, RoutedEventArgs e) { TreePopup.Visibility = TreePopup.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; if (TreePopup.Visibility == Visibility.Visible) SensorTreeSelector.PopulateTree(); }
    void CloseTreePopup_Click(object s, RoutedEventArgs e) => TreePopup.Visibility = Visibility.Collapsed;
    void SensorTreeSelector_SelectionChanged(string? id) { if (id == null) return; _bindingId = id; TreePopup.Visibility = Visibility.Collapsed; var sv = HardwareService.Instance.GetSensor(id); if (sv != null) { PushSensorValue(sv); RealSensorLabel.Text = sv.DisplayText; } else RealSensorLabel.Text = id; _realDataTimer.Start(); }
    void ClearBindBtn_Click(object s, RoutedEventArgs e) { _bindingId = null; _realDataTimer.Stop(); RealSensorLabel.Text = ""; SensorTreeSelector.SelectedBindingId = null; RefreshRender(); }

    void SelectFontItem(string family)
    {
        foreach (ComboBoxItem item in PropFontFamily.Items)
            if (item.Content?.ToString() == family) { item.IsSelected = true; return; }
    }

    void PropFontFamily_SelectionChanged(object s, SelectionChangedEventArgs e) { if (!_ready || _suppressSync) return; ApplyAndRefresh(); }

    public void StopTimers() { _chartTimer.Stop(); _realDataTimer.Stop(); }
}
