using System.IO;
using System.Text.Json;

namespace ScoutsReporter.Services;

public static class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScoutsReporter");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    public static bool LoadIsDarkMode()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return false;
            var json = File.ReadAllText(SettingsPath);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("isDarkMode", out var prop))
                return prop.GetBoolean();
        }
        catch
        {
            // Corrupt file — default to light
        }
        return false;
    }

    public static void SaveIsDarkMode(bool isDark)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(new { isDarkMode = isDark });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Best-effort persistence
        }
    }
}
