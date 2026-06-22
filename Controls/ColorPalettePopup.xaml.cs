using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SensorPanelToo.Controls;

public partial class ColorPalettePopup : Popup
{
    public event Action<string>? ColorSelected;

    private static readonly string[] _presetColors =
    {
        "#FF0000","#FF4400","#FF8800","#FFCC00","#FFFF00","#CCFF00","#88FF00","#44FF00","#00FF00","#00FF44",
        "#00FF88","#00FFCC","#00FFFF","#00CCFF","#0088FF","#0044FF","#0000FF","#4400FF","#8800FF","#CC00FF",
        "#FF00FF","#FF00CC","#FF0088","#FF0044","#FF6644","#00BFFF","#DC143C","#FFD700","#00FF88","#FF69B4",
        "#FF6347","#7FFFD4","#FFA500","#8A2BE2","#A52A2A","#5F9EA0","#7FFF00","#D2691E","#FF7F50","#6495ED",
        "#DC143C","#00CED1","#9400D3","#FF1493","#00BFFF","#696969","#1E90FF","#B22222","#228B22","#DAA520",
        "#4B0082","#ADFF2F","#F08080","#20B2AA","#87CEFA","#778899","#B0C4DE","#00FF7F","#4682B4","#D2B48C",
        "#FF4500","#DA70D6","#EEE8AA","#98FB98","#AFEEEE","#DB7093","#FFEFD5","#FFDAB9","#CD853F","#FFC0CB",
        "#800000","#808000","#008000","#800080","#008080","#000080","#FFFFFF","#C0C0C0","#808080","#404040",
        "#202020","#000000","#333333","#555555","#777777","#999999","#BBBBBB","#DDDDDD","#EEEEEE","#F5F5F5",
    };

    public ColorPalettePopup()
    {
        InitializeComponent();

        var items = _presetColors.Select(hex => new PaletteItem(hex)).ToList();
        PaletteGrid.ItemsSource = items;
    }

    public void SetCurrentColor(string hex)
    {
        HexBox.Text = hex.TrimStart('#');
        try { PreviewSwatch.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { }
    }

    private void OnColorClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Background is SolidColorBrush brush)
        {
            var hex = brush.Color.ToString();
            ColorSelected?.Invoke(hex);
            IsOpen = false;
        }
    }

    private void HexBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        var text = HexBox.Text.Trim();
        if (!text.StartsWith("#")) text = "#" + text;

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(text);
            var hex = color.ToString();
            PreviewSwatch.Background = new SolidColorBrush(color);
            ColorSelected?.Invoke(hex);
            IsOpen = false;
        }
        catch
        {
            HexBox.Text = "";
        }
    }

    private class PaletteItem
    {
        public Brush Brush { get; }
        public PaletteItem(string hex)
        {
            try { Brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
            catch { Brush = new SolidColorBrush(Colors.Magenta); }
        }
    }
}
