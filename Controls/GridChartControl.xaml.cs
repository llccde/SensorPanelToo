using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SensorPanelToo.Models;

namespace SensorPanelToo.Controls;

public partial class GridChartControl : UserControl
{
    public static readonly DependencyProperty ComponentDataProperty =
        DependencyProperty.Register(nameof(ComponentData), typeof(GridChartComponent), typeof(GridChartControl),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty SensorValueProperty =
        DependencyProperty.Register(nameof(SensorValue), typeof(SensorValue), typeof(GridChartControl),
            new PropertyMetadata(null, OnDataChanged));

    public GridChartComponent? ComponentData
    {
        get => (GridChartComponent?)GetValue(ComponentDataProperty);
        set => SetValue(ComponentDataProperty, value);
    }

    public SensorValue? SensorValue
    {
        get => (SensorValue?)GetValue(SensorValueProperty);
        set => SetValue(SensorValueProperty, value);
    }

    public GridChartControl()
    {
        InitializeComponent();
        Width = 400; Height = 200;
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (GridChartControl)d;
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
        DrawChart(dc);
    }

    private void DrawChart(DrawingContext dc)
    {
        var comp = ComponentData;
        if (comp == null) return;

        double w = Width;
        double h = Height;
        if (w <= 0 || h <= 0) return;

        double margin = 30;
        double chartLeft = margin;
        double chartTop = 5;
        double chartRight = w - 5;
        double chartBottom = h - 5;
        double chartWidth = chartRight - chartLeft;
        double chartHeight = chartBottom - chartTop;

        if (!comp.TransparentBackground)
            dc.DrawRectangle(new SolidColorBrush(ParseColor(comp.BackgroundColor)), null, new Rect(0, 0, w, h));

        dc.DrawRectangle(new SolidColorBrush(ParseColor(comp.ForegroundColor)), null,
            new Rect(chartLeft, chartTop, chartWidth, chartHeight));

        Color gridColor = ParseColor(comp.GridLineColor);
        var gridPen = new Pen(new SolidColorBrush(gridColor), comp.GridLineWidth);

        double yMin = 0, yMax = 100;
        var sv = SensorValue;
        if (sv != null && sv.UpperBound > sv.LowerBound) { yMin = sv.LowerBound; yMax = sv.UpperBound; }

        var labelTypeface = new Typeface(new FontFamily(comp.FontFamily), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var labelBrush = new SolidColorBrush(gridColor);

        for (int i = 0; i <= comp.GridDensityY; i++)
        {
            double y = chartTop + (chartHeight * i / comp.GridDensityY);
            dc.DrawLine(gridPen, new Point(chartLeft, y), new Point(chartRight, y));
            double val = yMax - ((yMax - yMin) * i / comp.GridDensityY);
            var label = new FormattedText($"{val:F0}", System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, labelTypeface, 9, labelBrush, 1.0);
            dc.DrawText(label, new Point(2, y - label.Height / 2));
        }

        for (int i = 0; i <= comp.GridDensityX; i++)
        {
            double x = chartLeft + (chartWidth * i / comp.GridDensityX);
            dc.DrawLine(gridPen, new Point(x, chartTop), new Point(x, chartBottom));
        }

        var history = comp.HistoryValues;
        if (history.Count < 2) return;

        var now = DateTime.Now;
        double timeWindow = comp.DurationSeconds;
        var visible = history.Where(pt => (now - pt.Time).TotalSeconds <= timeWindow).ToList();
        if (visible.Count < 2) return;

        double yRange = yMax - yMin;
        if (yRange <= 0) yRange = 1;

        Color lineColor = ParseColor(comp.LineColor);
        var linePen = new Pen(new SolidColorBrush(lineColor), comp.LineWidth);

        var points = new PointCollection();
        foreach (var pt in visible)
        {
            double x = chartRight - ((now - pt.Time).TotalSeconds / timeWindow) * chartWidth;
            double y = chartBottom - (((double)pt.Value - yMin) / yRange) * chartHeight;
            y = Math.Max(chartTop, Math.Min(chartBottom, y));
            points.Add(new Point(x, y));
        }

        if (comp.SmoothFactor > 0)
        {
            double sampleWindow = comp.SmoothFactor * 2.0;
            double sampleInterval = 0.05;
            int maxSamples = Math.Max(1, (int)Math.Ceiling(sampleWindow / sampleInterval));

            var sorted = history.OrderBy(pt => pt.Time).ToList();
            var times = new DateTime[sorted.Count];
            var values = new double[sorted.Count];
            for (int k = 0; k < sorted.Count; k++)
            {
                times[k] = sorted[k].Time;
                values[k] = sorted[k].Value;
            }

            var smoothed = new PointCollection();
            foreach (var pt in visible)
            {
                double weightedSum = 0, weightTotal = 0;
                for (int s = 0; s <= maxSamples; s++)
                {
                    double tOffset = s * sampleInterval;
                    var targetTime = pt.Time.AddSeconds(-tOffset);
                    double val = InterpolateValue(times, values, targetTime, pt.Value);
                    double weight = 1.0 / (1.0 + tOffset * tOffset);
                    weightedSum += val * weight;
                    weightTotal += weight;
                }

                double avgVal = weightTotal > 0 ? weightedSum / weightTotal : pt.Value;
                double y = chartBottom - ((avgVal - yMin) / yRange) * chartHeight;
                y = Math.Max(chartTop, Math.Min(chartBottom, y));
                double x = chartRight - ((now - pt.Time).TotalSeconds / timeWindow) * chartWidth;
                smoothed.Add(new Point(x, y));
            }

            for (int i = 0; i < smoothed.Count - 1; i++)
                dc.DrawLine(linePen, smoothed[i], smoothed[i + 1]);
            points = smoothed;
        }
        else
        {
            for (int i = 0; i < points.Count - 1; i++)
                dc.DrawLine(linePen, points[i], points[i + 1]);
        }

        if (comp.ShowFill && points.Count >= 2)
        {
            var fillGeo = new StreamGeometry();
            using (var ctx = fillGeo.Open())
            {
                ctx.BeginFigure(points[0], true, true);
                for (int i = 1; i < points.Count; i++) ctx.LineTo(points[i], true, false);
                ctx.LineTo(new Point(points[^1].X, chartBottom), true, false);
                ctx.LineTo(new Point(points[0].X, chartBottom), true, false);
            }
            var fillColor = lineColor; fillColor.A = (byte)(comp.FillOpacity * 255);
            dc.DrawGeometry(new SolidColorBrush(fillColor), null, fillGeo);
        }
    }

    private static Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Colors.Magenta; }
    }

    private static double InterpolateValue(DateTime[] times, double[] values, DateTime target, float fallback)
    {
        if (times.Length == 0) return fallback;
        int idx = Array.BinarySearch(times, target);
        if (idx >= 0) return values[idx];
        idx = ~idx;
        if (idx == 0) return values[0];
        if (idx >= times.Length) return values[^1];
        double frac = (target - times[idx - 1]).TotalSeconds / (times[idx] - times[idx - 1]).TotalSeconds;
        return values[idx - 1] + (values[idx] - values[idx - 1]) * frac;
    }
}
