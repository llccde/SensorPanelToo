using System.IO;
using System.Text.Json;

namespace SensorPanelToo.Services;

public static class ConfigService
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ThemesDirectory
    {
        get
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static void Save(Models.DashboardConfig config, string path)
    {
        var json = JsonSerializer.Serialize(config, _options);
        File.WriteAllText(path, json);
    }

    public static Models.DashboardConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Models.DashboardConfig>(json, _options)
               ?? new Models.DashboardConfig();
    }

    public static List<string> ListThemes()
    {
        if (!Directory.Exists(ThemesDirectory))
            return new List<string>();

        return Directory.GetFiles(ThemesDirectory, "*.json")
                        .Select(Path.GetFileNameWithoutExtension)
                        .ToList()!;
    }
}
