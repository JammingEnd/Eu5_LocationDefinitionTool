
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Eu5_MapTool.Settings;
public class Settings
{
    public string LastUsedDirectoryA { get; set; } = "";
    public string LastUsedDirectoryB { get; set; } = "";
}

public static class SettingsService
{
    private static readonly string AppFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MyAvaloniaApp"
    );

    private static readonly string SettingsFile = Path.Combine(AppFolder, "settings.json");

    public static async Task<Settings> LoadAsync()
    {
        try
        {
            if (!File.Exists(SettingsFile))
                return new Settings();

            string json = await File.ReadAllTextAsync(SettingsFile);
            return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
        }
        catch
        {
            return new Settings(); // fallback if file corrupt
        }
    }

    public static async Task SaveAsync(Settings settings)
    {
        if (!Directory.Exists(AppFolder))
            Directory.CreateDirectory(AppFolder);

        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(SettingsFile, json);
    }
}
