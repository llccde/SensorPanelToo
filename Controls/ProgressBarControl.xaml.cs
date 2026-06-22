using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SensorPanelToo.Models;

namespace SensorPanelToo.Controls;

public partial class ProgressBarControl : UserControl
{
    public static readonly DependencyProperty ComponentDataProperty =
        DependencyProperty.Register(nameof(ComponentData), typeof(ProgressBarComponent), typeof(ProgressBarControl),
            new PropertyMetadata(null, OnComponentDataChanged));

    public static readonly DependencyProperty SensorValueProperty =
        DependencyProperty.Register(nameof(SensorValue), typeof(SensorValue), typeof(ProgressBarControl),
            new PropertyMetadata(null, OnSensorValueChanged));

    public ProgressBarComponent? ComponentData
    {
        get => (ProgressBarComponent?)GetValue(ComponentDataProperty);
        set => SetValue(ComponentDataProperty, value);
    }

    public SensorValue? SensorValue
    {
        get => (SensorValue?)GetValue(SensorValueProperty);
        set => SetValue(SensorValueProperty, value);
    }

    public ProgressBarControl()
    {
        InitializeComponent();
        Width = 300; Height = 40;
    }

    private static void OnComponentDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (ProgressBarControl)d;
        ctrl.ApplyComponentData();
        ctrl.RefreshValue();
    }

    private static void OnSensorValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ProgressBarControl)d).RefreshValue();
    }

    public void ApplyComponentData()
    {
        var comp = ComponentData;
        if (comp == null) return;

        RenderTransform = new ScaleTransform(comp.Scale, comp.Scale);
        RenderTransformOrigin = new Point(0, 0);

        var fgColor = ParseColor(comp.ForegroundColor);
        var trackColor = comp.TransparentBackground ? Colors.Transparent : ParseColor(comp.BackgroundColor);
        var borderColor = ParseColor(comp.BorderColor);

        ProgressBar.Foreground = new SolidColorBrush(fgColor);
        ProgressBar.Background = new SolidColorBrush(trackColor);

        OuterBorder.BorderBrush = new SolidColorBrush(borderColor);
        OuterBorder.BorderThickness = new Thickness(comp.BorderThickness);
        OuterBorder.CornerRadius = new CornerRadius(comp.Roundness);

        double bt = comp.BorderThickness;
        LayoutRoot.Margin = new Thickness(bt);

        if (comp.Roundness > 0)
        {
            double innerR = Math.Max(0, comp.Roundness - bt);
            LayoutRoot.Clip = new RectangleGeometry(new Rect(0, 0, Width - bt * 2, Height - bt * 2), innerR, innerR);
        }
        else
            LayoutRoot.Clip = null;

        LayoutRoot.Background = comp.TransparentBackground ? null : new SolidColorBrush(ParseColor(comp.BackgroundColor));

        if (comp.Orientation == Models.Orientation.Vertical)
        {
            ProgressBar.RenderTransform = new RotateTransform(-90);
            ProgressBar.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        else
        {
            ProgressBar.RenderTransform = null;
        }

        ValueText.Foreground = new SolidColorBrush(ParseColor(comp.TextColor));
        ValueText.FontFamily = new FontFamily(comp.FontFamily);
        ValueText.FontSize = comp.FontSize;

        ValueText.Visibility = comp.ShowValueText ? Visibility.Visible : Visibility.Collapsed;
        ValueText.VerticalAlignment = comp.ValueTextPosition == ValueTextPosition.Outside
            ? VerticalAlignment.Bottom : VerticalAlignment.Center;
    }

    public void RefreshValue()
    {
        var sv = SensorValue;
        var comp = ComponentData;
        if (comp == null) return;

        if (sv != null)
        {
            double range = (double)(sv.UpperBound - sv.LowerBound);
            double value = (double)(sv.CurrentValue - sv.LowerBound);
            double percent = range > 0 ? (value / range) * 100 : 0;
            ProgressBar.Value = Math.Max(0, Math.Min(100, percent));
            ValueText.Text = sv.DisplayText;
        }
        else
        {
            ProgressBar.Value = 0;
            ValueText.Text = "N/A";
        }
    }

    private static Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Colors.Magenta; }
    }
}
