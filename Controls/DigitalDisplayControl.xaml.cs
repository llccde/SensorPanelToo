using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SensorPanelToo.Models;

namespace SensorPanelToo.Controls;

public partial class DigitalDisplayControl : UserControl
{
    private const double BASE_FONT_SIZE = 36.0;
    private const double PADDING = 8.0;
    private const double LINE_SPACING = 1.15;

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
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (DigitalDisplayControl)d;
        ctrl.InvalidateMeasure();
        ctrl.InvalidateVisual();
    }

    public void Refresh()
    {
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var comp = ComponentData;
        if (comp == null) return base.MeasureOverride(constraint);

        double fontSize = BASE_FONT_SIZE * comp.Scale;
        var lines = BuildDisplayLines();
        var typeface = GetTypeface(comp);

        double maxWidth = 0;
        double lineHeight = 0;
        foreach (var line in lines)
        {
            var ft = new FormattedText(line, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, fontSize, Brushes.White, 1.0);
            if (ft.Width > maxWidth) maxWidth = ft.Width;
            if (ft.Height > lineHeight) lineHeight = ft.Height;
        }

        double w = maxWidth + comp.StrokeThickness * 2 + PADDING * 2;
        double h = lineHeight * LINE_SPACING * lines.Count + comp.StrokeThickness * 2 + PADDING * 2;
        return new Size(Math.Max(20, w), Math.Max(20, h));
    }

    private List<string> BuildDisplayLines()
    {
        var comp = ComponentData;
        var sv = SensorValue;

        string fullText;
        if (sv != null)
        {
            string formatted = sv.CurrentValue.ToString($"F{comp!.DecimalPlaces}");
            string prefix = comp.ShowPrefix ? (sv.Unit + " ") : "";
            string suffix = comp.ShowSuffix ? (" " + sv.Unit) : "";
            fullText = prefix + formatted + suffix;
        }
        else
        {
            fullText = "N/A";
        }

        return SplitIntoLines(fullText.Trim(), comp!.LineCount);
    }

    private static List<string> SplitIntoLines(string text, int lineCount)
    {
        if (lineCount <= 1 || string.IsNullOrWhiteSpace(text))
            return new List<string> { text };

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= lineCount)
        {
            var result = new List<string>();
            foreach (var w in words) result.Add(w);
            while (result.Count < lineCount) result.Add("");
            return result;
        }

        var lines = new List<string>();
        int wordsPerLine = (int)Math.Ceiling((double)words.Length / lineCount);
        for (int i = 0; i < lineCount; i++)
        {
            int start = i * wordsPerLine;
            if (start >= words.Length) break;
            int count = Math.Min(wordsPerLine, words.Length - start);
            lines.Add(string.Join(" ", words.Skip(start).Take(count)));
        }
        return lines;
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

        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        if (!comp.TransparentBackground)
            dc.DrawRectangle(new SolidColorBrush(ParseColor(comp.BackgroundColor)), null, new Rect(0, 0, w, h));

        double fontSize = BASE_FONT_SIZE * comp.Scale;
        var typeface = GetTypeface(comp);
        var fgBrush = new SolidColorBrush(ParseColor(comp.ForegroundColor));
        var lines = BuildDisplayLines();

        double lineHeight = 0;
        foreach (var line in lines)
        {
            var ft = new FormattedText(line, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, fontSize, fgBrush, 1.0);
            if (ft.Height > lineHeight) lineHeight = ft.Height;
        }

        double totalTextHeight = lineHeight * LINE_SPACING * lines.Count;
        double startY = (h - totalTextHeight) / 2;

        for (int i = 0; i < lines.Count; i++)
        {
            var ft = new FormattedText(lines[i], CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, fontSize, fgBrush, 1.0)
            { TextAlignment = TextAlignment.Center };

            double x = (w - ft.Width) / 2 + comp.StrokeThickness / 2;
            double y = startY + i * lineHeight * LINE_SPACING;

            Geometry textGeometry = ft.BuildGeometry(new Point(x, y));

            if (comp.StrokeThickness > 0)
            {
                var strokeBrush = new SolidColorBrush(ParseColor(comp.StrokeColor));
                dc.DrawGeometry(null, new Pen(strokeBrush, comp.StrokeThickness) { LineJoin = PenLineJoin.Round }, textGeometry);
            }

            dc.DrawGeometry(fgBrush, null, textGeometry);
        }
    }

    private static Typeface GetTypeface(DigitalDisplayComponent comp)
    {
        var ff = new FontFamily(comp.FontFamily);
        var fw = comp.FontWeight == FontWeightOption.Bold ? FontWeights.Bold : FontWeights.Normal;
        return new Typeface(ff, FontStyles.Normal, fw, FontStretches.Normal);
    }

    private static Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Colors.Magenta; }
    }
}
