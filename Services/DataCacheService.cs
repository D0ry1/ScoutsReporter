using ScoutsReporter.Models;

namespace ScoutsReporter.Services;

public class DataCacheService
{
    private readonly ApiService _api;
    private readonly Func<List<UnitInfo>> _getSelectedUnits;

    public List<UnitInfo>? Units { get; private set; }
    public List<TeamInfo>? Teams { get; private set; }
    public Dictionary<string, Member>? Members { get; private set; }
    public bool IsLoaded => Members != null;

    public DataCacheService(ApiService api, Func<List<UnitInfo>> getSelectedUnits)
    {
        _api = api;
        _getSelectedUnits = getSelectedUnits;
    }

    public async Task EnsureLoadedAsync(IProgress<string>? progress, CancellationToken ct)
    {
        if (IsLoaded) return;

        progress?.Report("Discovering units...");
        Units = _getSelectedUnits();
        if (Units.Count == 0)
            throw new InvalidOperationException("No units selected. Please select at least one unit.");
        ct.ThrowIfCancellationRequested();

        progress?.Report("Discovering teams...");
        Teams = await _api.FetchTeamsAsync(Units);
        ct.ThrowIfCancellationRequested();

        progress?.Report("Fetching members...");
        Members = await _api.FetchTeamMembersAsync(Teams, progress);
        ct.ThrowIfCancellationRequested();
    }

    public void Invalidate()
    {
        Units = null;
        Teams = null;
        Members = null;
    }
}
