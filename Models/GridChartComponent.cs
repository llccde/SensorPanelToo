namespace SensorPanelToo.Models;

public class GridChartComponent : Component
{
    public int DurationSeconds { get; set; } = 60;
    public int GridDensityX { get; set; } = 5;
    public int GridDensityY { get; set; } = 5;
    public string GridLineColor { get; set; } = "#E0E0E0";
    public double GridLineWidth { get; set; } = 0.5;
    public double LineWidth { get; set; } = 2;
    public string LineColor { get; set; } = "#0066CC";
    public double SmoothFactor { get; set; }
    public bool ShowFill { get; set; }
    public double FillOpacity { get; set; } = 0.2;

    public double BaseWidth => 400;
    public double BaseHeight => 200;

    public List<(DateTime Time, float Value)> HistoryValues { get; set; } = new();

    public GridChartComponent()
    {
        ComponentType = ComponentType.GridChart;
    }
}
