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
        var existing = LoadAll();
        existing["isDarkMode"] = isDark;
        SaveAll(existing);
    }

    public static string? LoadDismissedUpdateVersion()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return null;
            var json = File.ReadAllText(SettingsPath);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("dismissedUpdateVersion", out var prop))
                return prop.GetString();
        }
        catch
        {
            // Corrupt file
        }
        return null;
    }

    public static void SaveDismissedUpdateVersion(string? version)
    {
        var existing = LoadAll();
        if (version != null)
            existing["dismissedUpdateVersion"] = version;
        else
            existing.Remove("dismissedUpdateVersion");
        SaveAll(existing);
    }

    private static Dictionary<string, object> LoadAll()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (dict != null)
                {
                    var result = new Dictionary<string, object>();
                    foreach (var kvp in dict)
                    {
                        result[kvp.Key] = kvp.Value.ValueKind switch
                        {
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String => kvp.Value.GetString()!,
                            JsonValueKind.Number => kvp.Value.GetDouble(),
                            _ => kvp.Value.GetRawText()
                        };
                    }
                    return result;
                }
            }
        }
        catch
        {
            // Fall through
        }
        return new Dictionary<string, object>();
    }

    private static void SaveAll(Dictionary<string, object> data)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Best-effort persistence
        }
    }
}
