namespace SensorPanelToo.Models;

public enum SensorValueType
{
    Continuous,
    Discrete,
    Enum
}

public class SensorValue
{
    public string BindingId { get; set; } = "";
    public float CurrentValue { get; set; }
    public string DisplayText { get; set; } = "";
    public string Unit { get; set; } = "";
    public SensorValueType ValueType { get; set; }
    public float UpperBound { get; set; }
    public float LowerBound { get; set; }
    public float[]? DiscreteValues { get; set; }
    public Dictionary<float, string>? EnumMap { get; set; }
}

public class SensorTreeNode
{
    public string Name { get; set; } = "";
    public string? BindingId { get; set; }
    public string? SensorType { get; set; }
    public List<SensorTreeNode> Children { get; set; } = new();
    public string? Unit { get; set; }
    public (float Min, float Max)? ValueRange { get; set; }
}
