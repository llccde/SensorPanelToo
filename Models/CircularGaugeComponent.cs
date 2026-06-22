namespace SensorPanelToo.Models;

public enum GaugeStyle
{
    Solid,
    Dashed
}

public enum NeedleStyle
{
    Retro,
    Plastic,
    Metal,
    ScanLine,
    Finger
}

public class CircularGaugeComponent : Component
{
    public double SweepAngle { get; set; } = 270;
    public double StartAngle { get; set; } = -135;
    public GaugeStyle GaugeStyle { get; set; } = GaugeStyle.Solid;
    public bool NeedleEnabled { get; set; } = true;
    public string NeedleColor { get; set; } = "#FF0000";
    public double NeedleWidth { get; set; } = 2.5;
    public NeedleStyle NeedleStyle { get; set; } = NeedleStyle.Metal;
    public double RingThickness { get; set; } = 12;
    public bool ShowCenterValue { get; set; } = true;
    public string TextColor { get; set; } = "#FFFFFF";

    public double BaseSize => 200;

    public CircularGaugeComponent()
    {
        ComponentType = ComponentType.CircularGauge;
    }
}
