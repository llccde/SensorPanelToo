using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SensorPanelToo.Models;

namespace SensorPanelToo.Controls;

public partial class CircularGaugeControl : UserControl
{
    public static readonly DependencyProperty ComponentDataProperty =
        DependencyProperty.Register(nameof(ComponentData), typeof(CircularGaugeComponent), typeof(CircularGaugeControl),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty SensorValueProperty =
        DependencyProperty.Register(nameof(SensorValue), typeof(SensorValue), typeof(CircularGaugeControl),
            new PropertyMetadata(null, OnDataChanged));

    public CircularGaugeComponent? ComponentData
    {
        get => (CircularGaugeComponent?)GetValue(ComponentDataProperty);
        set => SetValue(ComponentDataProperty, value);
    }

    public SensorValue? SensorValue
    {
        get => (SensorValue?)GetValue(SensorValueProperty);
        set => SetValue(SensorValueProperty, value);
    }

    public CircularGaugeControl()
    {
        InitializeComponent();
        Width = 200; Height = 200;
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (CircularGaugeControl)d;
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

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        DrawGauge(dc);
    }

    private void DrawGauge(DrawingContext dc)
    {
        var comp = ComponentData;
        if (comp == null) return;

        double w = Width;
        double h = Height;
        if (w <= 0 || h <= 0) return;

        double size = Math.Min(w, h);
        double cx = w / 2;
        double cy = h / 2;
        double ringThickness = comp.RingThickness;
        double outerRadius = (size / 2) - ringThickness / 2 - 2;
        double innerRadius = outerRadius - ringThickness;

        Color fgColor = ParseColor(comp.ForegroundColor);

        if (!comp.TransparentBackground)
            dc.DrawRectangle(new SolidColorBrush(ParseColor(comp.BackgroundColor)), null, new Rect(0, 0, w, h));

        double startAngle = comp.StartAngle;
        double sweepAngle = comp.SweepAngle;

        // Track arc
        if (!comp.HideTrack)
        {
            var trackPen = new Pen(new SolidColorBrush(ParseColor(comp.TrackColor)), ringThickness)
            {
                StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round
            };
            DrawArc(dc, trackPen, new Point(cx, cy), outerRadius - ringThickness / 2, startAngle, startAngle + sweepAngle);
        }

        // Value arc
        double percent = GetPercent();
        double valueSweep = sweepAngle * percent;
        if (valueSweep > 0.5)
        {
            var valuePen = new Pen(new SolidColorBrush(fgColor), ringThickness)
            {
                StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round
            };
            DrawArc(dc, valuePen, new Point(cx, cy), outerRadius - ringThickness / 2, startAngle, startAngle + valueSweep);
        }

        // Needle
        if (comp.NeedleEnabled)
        {
            double needleAngle = startAngle + (sweepAngle * percent);
            double needleRad = DegreesToRadians(needleAngle);
            double needleLength = outerRadius - ringThickness;
            double nx = cx + Math.Cos(needleRad) * needleLength;
            double ny = cy + Math.Sin(needleRad) * needleLength;

            var needlePen = new Pen(new SolidColorBrush(ParseColor(comp.NeedleColor)), comp.NeedleWidth)
            {
                StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round
            };
            dc.DrawLine(needlePen, new Point(cx, cy), new Point(nx, ny));

            double dotRadius = ringThickness / 3;
            dc.DrawEllipse(new SolidColorBrush(ParseColor(comp.NeedleColor)), null,
                new Point(cx, cy), dotRadius, dotRadius);
        }

        // Center text
        if (comp.ShowCenterValue)
        {
            string text = SensorValue?.DisplayText ?? "N/A";
            var typeface = new Typeface(new FontFamily(comp.FontFamily), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var fgBrush = new SolidColorBrush(ParseColor(comp.TextColor));
            double maxTextWidth = innerRadius * 1.4;
            var ft = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, comp.FontSize, fgBrush, 1.0)
            {
                MaxTextWidth = maxTextWidth, TextAlignment = TextAlignment.Center
            };
            dc.DrawText(ft, new Point(cx - ft.Width / 2, cy - ft.Height / 2));
        }
    }

    private double GetPercent()
    {
        var sv = SensorValue;
        if (sv == null) return 0;
        double range = (double)(sv.UpperBound - sv.LowerBound);
        if (range <= 0) return 0;
        return Math.Max(0, Math.Min(1, ((double)sv.CurrentValue - (double)sv.LowerBound) / range));
    }

    private static void DrawArc(DrawingContext dc, Pen pen, Point center, double radius, double startDeg, double endDeg)
    {
        double sweep = endDeg - startDeg;
        if (Math.Abs(sweep) < 0.001) return;
        if (Math.Abs(sweep) >= 360)
        {
            dc.DrawEllipse(null, pen, center, radius, radius);
            return;
        }
        double sr = DegreesToRadians(startDeg), er = DegreesToRadians(endDeg);
        bool large = sweep > 180;
        double sx = center.X + Math.Cos(sr) * radius, sy = center.Y + Math.Sin(sr) * radius;
        double ex = center.X + Math.Cos(er) * radius, ey = center.Y + Math.Sin(er) * radius;
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(new Point(sx, sy), false, false);
            ctx.ArcTo(new Point(ex, ey), new Size(radius, radius), 0, large, SweepDirection.Clockwise, true, false);
        }
        dc.DrawGeometry(null, pen, geo);
    }

    private static double DegreesToRadians(double d) => d * Math.PI / 180.0;

    private static Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Colors.Magenta; }
    }
}
