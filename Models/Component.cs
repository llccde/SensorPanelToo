using System.Text.Json.Serialization;

namespace SensorPanelToo.Models;

public enum ComponentType
{
    ProgressBar,
    CircularGauge,
    DigitalDisplay,
    GridChart,
    SensorLabel
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ProgressBarComponent), typeDiscriminator: "ProgressBar")]
[JsonDerivedType(typeof(CircularGaugeComponent), typeDiscriminator: "CircularGauge")]
[JsonDerivedType(typeof(DigitalDisplayComponent), typeDiscriminator: "DigitalDisplay")]
[JsonDerivedType(typeof(GridChartComponent), typeDiscriminator: "GridChart")]
[JsonDerivedType(typeof(SensorLabelComponent), typeDiscriminator: "SensorLabel")]
public class Component
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ComponentType ComponentType { get; set; }
    public string? BindingId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Scale { get; set; } = 1.0;
    public int ZIndex { get; set; }
    public string ForegroundColor { get; set; } = "#00FF88";
    public string BackgroundColor { get; set; } = "#00000000";
    public bool TransparentBackground { get; set; } = true;
    public string FontFamily { get; set; } = "Consolas";
    public double FontSize { get; set; } = 14;
}
