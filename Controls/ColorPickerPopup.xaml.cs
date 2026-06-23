using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SensorPanelToo.Controls;

public partial class ColorPickerPopup : Popup
{
    public event Action<string>? ColorChanged;
    public event Action<string>? ColorSelected;
    public event Action<string>? ColorCancelled;

    private double _hue;
    private double _saturation = 1.0;
    private double _value = 1.0;
    private bool _isDraggingSv;
    private bool _isDraggingHue;
    private string _startColor = "#FFFFFF";
    private bool _suppress;

    public ColorPickerPopup()
    {
        InitializeComponent();
    }

    public void SetCurrentColor(string hex)
    {
        _startColor = hex;
        var c = ParseColor(hex);
        SwatchPreview.Background = new SolidColorBrush(c);
        SwatchOld.Background = new SolidColorBrush(c);
        (double h, double s, double v) = ColorToHsv(c);
        _hue = h;
        _saturation = s;
        _value = v;
        UpdateHuePlane();
        UpdateMarkers();
        UpdateHex();
    }

    Color CurrentColor => HsvToColor(_hue, _saturation, _value);

    void RestoreStartColor()
    {
        var c = ParseColor(_startColor);
        (double h, double s, double v) = ColorToHsv(c);
        _hue = h;
        _saturation = s;
        _value = v;
        UpdateHuePlane();
        UpdateMarkers();
        SwatchPreview.Background = new SolidColorBrush(c);
    }

    void UpdateHuePlane()
    {
        SvHueBrush.Color = HsvToColor(_hue, 1.0, 1.0);
    }

    void UpdateMarkers()
    {
        double svX = _saturation * 200 - 5;
        double svY = (1 - _value) * 200 - 5;
        SvMarker.Margin = new Thickness(svX, svY, 0, 0);

        double hueY = _hue / 360.0 * 200 - 2;
        HueMarker.Margin = new Thickness(0, hueY, 0, 0);
    }

    void UpdateHex()
    {
        if (_suppress) return;
        _suppress = true;
        var c = CurrentColor;
        HexBox.Text = c.ToString().TrimStart('#');
        SwatchPreview.Background = new SolidColorBrush(c);
        ColorChanged?.Invoke("#" + HexBox.Text);
        _suppress = false;
    }

    void ApplyFromHex()
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString("#" + HexBox.Text);
            (double h, double s, double v) = ColorToHsv(c);
            _hue = h;
            _saturation = s;
            _value = v;
            UpdateHuePlane();
            UpdateMarkers();
            SwatchPreview.Background = new SolidColorBrush(c);
            ColorChanged?.Invoke("#" + HexBox.Text);
        }
        catch { }
    }

    void SvPlane_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSv = true;
        var pos = e.GetPosition((UIElement)sender);
        _saturation = Math.Clamp(pos.X / 200, 0, 1);
        _value = Math.Clamp(1 - pos.Y / 200, 0, 1);
        UpdateMarkers();
        UpdateHex();
        CaptureMouse();
    }

    void SvPlane_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingSv) return;
        var pos = e.GetPosition((UIElement)sender);
        _saturation = Math.Clamp(pos.X / 200, 0, 1);
        _value = Math.Clamp(1 - pos.Y / 200, 0, 1);
        UpdateMarkers();
        UpdateHex();
    }

    void SvPlane_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSv = false;
        ReleaseMouseCapture();
    }

    void HueBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingHue = true;
        var pos = e.GetPosition((UIElement)sender);
        _hue = Math.Clamp(pos.Y / 200 * 360, 0, 360);
        UpdateHuePlane();
        UpdateMarkers();
        UpdateHex();
        CaptureMouse();
    }

    void HueBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingHue) return;
        var pos = e.GetPosition((UIElement)sender);
        _hue = Math.Clamp(pos.Y / 200 * 360, 0, 360);
        UpdateHuePlane();
        UpdateMarkers();
        UpdateHex();
    }

    void HueBar_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingHue = false;
        ReleaseMouseCapture();
    }

    void HexBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyFromHex();
            IsOpen = false;
        }
    }

    void OkBtn_Click(object sender, RoutedEventArgs e)
    {
        var hex = "#" + HexBox.Text;
        ColorSelected?.Invoke(hex);
        IsOpen = false;
    }

    void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        RestoreStartColor();
        ColorCancelled?.Invoke(_startColor);
        IsOpen = false;
    }

    void Popup_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RestoreStartColor();
            ColorCancelled?.Invoke(_startColor);
            IsOpen = false;
            e.Handled = true;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var border = Child as Border;
        border?.Focus();
    }

    static (double h, double s, double v) ColorToHsv(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;
        double h = 0;
        if (delta > 0.0001)
        {
            if (max == r) h = 60 * (((g - b) / delta) % 6);
            else if (max == g) h = 60 * ((b - r) / delta + 2);
            else h = 60 * ((r - g) / delta + 4);
        }
        if (h < 0) h += 360;
        double s = max > 0.0001 ? delta / max : 0;
        double v = max;
        return (h, s, v);
    }

    static Color HsvToColor(double h, double s, double v)
    {
        h %= 360; if (h < 0) h += 360;
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;
        (double r, double g, double b) = (h switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0, c, x),
            < 240 => (0, x, c),
            < 300 => (x, 0, c),
            _ => (c, 0, x)
        });
        return Color.FromRgb(
            (byte)Math.Clamp((r + m) * 255, 0, 255),
            (byte)Math.Clamp((g + m) * 255, 0, 255),
            (byte)Math.Clamp((b + m) * 255, 0, 255));
    }

    static Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Color.FromRgb(255, 0, 0); }
    }
}
