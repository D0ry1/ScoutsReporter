using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace ScoutsReporter.Services;

public record UpdateInfo(string NewVersion, string ReleaseUrl, string ReleaseNotes);

public static class UpdateService
{
    private static readonly HttpClient _http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("ScoutsReporter", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0"));
        return client;
    }

    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(
                "https://api.github.com/repos/D0ry1/ScoutsReporter/releases/latest");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString();
            if (string.IsNullOrWhiteSpace(tagName))
                return null;

            var versionString = tagName.TrimStart('v', 'V');
            if (!Version.TryParse(versionString, out var remoteVersion))
                return null;

            var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (localVersion == null || remoteVersion <= localVersion)
                return null;

            var releaseUrl = root.GetProperty("html_url").GetString() ?? "";
            var releaseNotes = root.TryGetProperty("body", out var bodyProp)
                ? bodyProp.GetString() ?? ""
                : "";

            return new UpdateInfo(versionString, releaseUrl, releaseNotes);
        }
        catch
        {
            return null;
        }
    }
}
