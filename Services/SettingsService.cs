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

    public static T? LoadObject<T>(string key) where T : class
    {
        try
        {
            if (!File.Exists(SettingsPath)) return null;
            var json = File.ReadAllText(SettingsPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dict != null && dict.TryGetValue(key, out var element))
                return JsonSerializer.Deserialize<T>(element.GetRawText());
        }
        catch
        {
            // Corrupt file
        }
        return null;
    }

    public static void SaveObject<T>(string key, T value) where T : class
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            Dictionary<string, JsonElement> dict;
            if (File.Exists(SettingsPath))
            {
                var existing = File.ReadAllText(SettingsPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existing)
                       ?? new Dictionary<string, JsonElement>();
            }
            else
            {
                dict = new Dictionary<string, JsonElement>();
            }

            var serialized = JsonSerializer.Serialize(value);
            dict[key] = JsonDocument.Parse(serialized).RootElement.Clone();

            var options = new JsonSerializerOptions { WriteIndented = false };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(dict, options));
        }
        catch
        {
            // Best-effort persistence
        }
    }

    public static void RemoveKey(string key)
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dict != null && dict.Remove(key))
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(dict));
        }
        catch
        {
            // Best-effort
        }
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
