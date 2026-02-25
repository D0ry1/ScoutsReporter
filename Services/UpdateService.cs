using Velopack;
using Velopack.Sources;

namespace ScoutsReporter.Services;

public record AppUpdateInfo(string NewVersion, string ReleaseNotes);

public class UpdateService
{
    private readonly UpdateManager _um;
    private Velopack.UpdateInfo? _velopackUpdate;

    public UpdateService()
    {
        _um = new UpdateManager(
            new GithubSource("https://github.com/D0ry1/ScoutsReporter", null, false));
    }

    public bool IsInstalled => _um.IsInstalled;

    public async Task<AppUpdateInfo?> CheckForUpdateAsync()
    {
        if (!_um.IsInstalled)
            return null;

        try
        {
            _velopackUpdate = await _um.CheckForUpdatesAsync();
            if (_velopackUpdate == null)
                return null;

            return new AppUpdateInfo(
                _velopackUpdate.TargetFullRelease.Version.ToString(),
                _velopackUpdate.TargetFullRelease.NotesMarkdown ?? "");
        }
        catch
        {
            return null;
        }
    }

    public async Task DownloadUpdatesAsync(Action<int>? progress = null)
    {
        if (_velopackUpdate == null)
            throw new InvalidOperationException("No update available. Call CheckForUpdateAsync first.");

        await _um.DownloadUpdatesAsync(_velopackUpdate, progress);
    }

    public void ApplyAndRestart()
    {
        _um.ApplyUpdatesAndRestart(null);
    }
}
