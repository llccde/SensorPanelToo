namespace SensorPanelToo.Models;

public enum FontWeightOption
{
    Normal,
    Bold
}

public class DigitalDisplayComponent : Component
{
    public bool ShowPrefix { get; set; }
    public bool ShowSuffix { get; set; } = true;
    public int DecimalPlaces { get; set; } = 1;
    public string StrokeColor { get; set; } = "#CCCCCC";
    public double StrokeThickness { get; set; } = 1;
    public FontWeightOption FontWeight { get; set; } = FontWeightOption.Bold;
    public int LineCount { get; set; } = 1;

    public double BaseWidth => 250;
    public double BaseHeight => 80;

    public DigitalDisplayComponent()
    {
        ComponentType = ComponentType.DigitalDisplay;
    }
}
