namespace SensorPanelToo.Models;

public enum Orientation
{
    Horizontal,
    Vertical
}

public enum ValueTextPosition
{
    Inside,
    Outside
}

public class ProgressBarComponent : Component
{
    public string ProgressColor { get; set; } = "#00FF88";
    public string TrackColor { get; set; } = "#E0E0E0";
    public Orientation Orientation { get; set; } = Orientation.Horizontal;
    public bool ShowValueText { get; set; } = true;
    public ValueTextPosition ValueTextPosition { get; set; } = ValueTextPosition.Inside;
    public string TextColor { get; set; } = "#FFFFFF";
    public double BorderThickness { get; set; }
    public string BorderColor { get; set; } = "#CCCCCC";
    public double Roundness { get; set; }

    public double BaseWidth => 300;
    public double BaseHeight => 40;

    public ProgressBarComponent()
    {
        ComponentType = ComponentType.ProgressBar;
    }
}
