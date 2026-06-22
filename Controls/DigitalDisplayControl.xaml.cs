using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SensorPanelToo.Models;

namespace SensorPanelToo.Controls;

public partial class DigitalDisplayControl : UserControl
{
    public static readonly DependencyProperty ComponentDataProperty =
        DependencyProperty.Register(nameof(ComponentData), typeof(DigitalDisplayComponent), typeof(DigitalDisplayControl),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty SensorValueProperty =
        DependencyProperty.Register(nameof(SensorValue), typeof(SensorValue), typeof(DigitalDisplayControl),
            new PropertyMetadata(null, OnDataChanged));

    public DigitalDisplayComponent? ComponentData
    {
        get => (DigitalDisplayComponent?)GetValue(ComponentDataProperty);
        set => SetValue(ComponentDataProperty, value);
    }

    public SensorValue? SensorValue
    {
        get => (SensorValue?)GetValue(SensorValueProperty);
        set => SetValue(SensorValueProperty, value);
    }

    public DigitalDisplayControl()
    {
        InitializeComponent();
        Width = 250; Height = 80;
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (DigitalDisplayControl)d;
        ctrl.UpdateTransform();
        ctrl.InvalidateVisual();
    }

    private void UpdateTransform()
    {
        var comp = ComponentData;
        if (comp == null) return;
        RenderTransform = new ScaleTransform(comp.Scale, comp.Scale);
        RenderTransformOrigin = new Point(0, 0);
    }

    public void Refresh()
    {
        UpdateTransform();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        DrawDisplay(dc);
    }

    private void DrawDisplay(DrawingContext dc)
    {
        var comp = ComponentData;
        if (comp == null) return;

        double w = Width;
        double h = Height;
        if (w <= 0 || h <= 0) return;

        if (!comp.TransparentBackground)
            dc.DrawRectangle(new SolidColorBrush(ParseColor(comp.BackgroundColor)), null, new Rect(0, 0, w, h));

        string text;
        var sv = SensorValue;

        if (sv != null)
        {
            string formatted = sv.CurrentValue.ToString($"F{comp.DecimalPlaces}");
            string prefix = comp.ShowPrefix ? (sv.Unit + " ") : "";
            string suffix = comp.ShowSuffix ? (" " + sv.Unit) : "";
            text = prefix + formatted + suffix;
        }
        else
        {
            text = "N/A";
        }

        var fontFamily = new FontFamily(comp.FontFamily);
        var fontWeight = comp.FontWeight == FontWeightOption.Bold ? FontWeights.Bold : FontWeights.Normal;
        var typeface = new Typeface(fontFamily, FontStyles.Normal, fontWeight, FontStretches.Normal);
        var fgBrush = new SolidColorBrush(ParseColor(comp.ForegroundColor));

        var ft = new FormattedText(text, CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, typeface, comp.FontSize, fgBrush, 1.0)
        {
            TextAlignment = TextAlignment.Center,
            MaxTextWidth = w * 0.9
        };

        double x = (w - ft.Width) / 2;
        double y = (h - ft.Height) / 2;

        Geometry textGeometry = ft.BuildGeometry(new Point(x, y));

        if (comp.StrokeThickness > 0)
        {
            var strokeBrush = new SolidColorBrush(ParseColor(comp.StrokeColor));
            dc.DrawGeometry(null, new Pen(strokeBrush, comp.StrokeThickness) { LineJoin = PenLineJoin.Round }, textGeometry);
        }

        dc.DrawGeometry(fgBrush, null, textGeometry);
    }

    private static Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Colors.Magenta; }
    }
}
