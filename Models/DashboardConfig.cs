namespace SensorPanelToo.Models;

public class DashboardConfig
{
    public string Version { get; set; } = "1.0";
    public string ThemeName { get; set; } = "Default";
    public double CanvasWidth { get; set; } = 1280;
    public double CanvasHeight { get; set; } = 720;
    public string BackgroundColor { get; set; } = "#0A0A0F";
    public string BackgroundImagePath { get; set; } = "";
    public double BackgroundImageScale { get; set; } = 1.0;
    public double BackgroundImageOffsetX { get; set; }
    public double BackgroundImageOffsetY { get; set; }
    public List<Component> Components { get; set; } = new();
}
