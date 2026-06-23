namespace SensorPanelToo.Models;

public class SensorLabelComponent : Component
{
    public bool ShowPrefix { get; set; }
    public bool ShowSuffix { get; set; } = true;
    public int HierarchyLevels { get; set; } = 3;
    public string StrokeColor { get; set; } = "#CCCCCC";
    public double StrokeThickness { get; set; } = 1;
    public FontWeightOption FontWeight { get; set; } = FontWeightOption.Bold;

    public double BaseWidth => 300;
    public double BaseHeight => 60;

    public SensorLabelComponent()
    {
        ComponentType = ComponentType.SensorLabel;
    }
}
