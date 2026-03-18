using System.Collections.ObjectModel;
using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScoutsReporter.Models;
using ScoutsReporter.Services;

namespace ScoutsReporter.ViewModels;

public partial class TrainingReportViewModel : ObservableObject
{
    private static readonly HashSet<string> ExpiringTrainings = new()
    {
        "First Response", "Safeguarding", "Safety"
    };

    private readonly ApiService _api;
    private readonly AuthService _auth;
    private readonly TrainingService _trainingService;
    private readonly DataCacheService _cache;
    private readonly MainViewModel _main;

    [ObservableProperty]
    private string _statusText = "Click 'Run Report' to generate the Training report.";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private DataView? _reportData;

    private List<TrainingReportRow> _reportRows = new();

    public IReadOnlyList<TrainingReportRow> ReportRowsReadOnly => _reportRows;
    private List<string> _sortedTitles = new();

    [ObservableProperty] private int _totalMembers;
    [ObservableProperty] private int _okCount;
    [ObservableProperty] private int _attentionCount;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private bool _hideNonMemberDisclosure;

    partial void OnSearchTextChanged(string value) => ApplySearchFilter();
    partial void OnHideNonMemberDisclosureChanged(bool value) => ApplySearchFilter();

    public TrainingReportViewModel(ApiService api, AuthService auth, TrainingService trainingService, DataCacheService cache, MainViewModel main)
    {
        _api = api;
        _auth = auth;
        _trainingService = trainingService;
        _cache = cache;
        _main = main;
    }

    public void ClearReport()
    {
        ReportData = null;
        _reportRows.Clear();
        _sortedTitles.Clear();
        TotalMembers = 0;
        OkCount = 0;
        AttentionCount = 0;
        SearchText = "";
        HideNonMemberDisclosure = false;
        StatusText = "Click 'Run Report' to generate the Training report.";
        ProgressText = "";
    }

    private void ApplySearchFilter()
    {
        if (ReportData == null) return;

        var filters = new List<string>();

        if (HideNonMemberDisclosure)
            filters.Add("NOT (Roles LIKE '%Non Member - Needs Disclosure%' AND Roles NOT LIKE '%;%')");

        var search = SearchText.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var escaped = search.Replace("'", "''");
            filters.Add($"(Name LIKE '%{escaped}%' OR Flag LIKE '%{escaped}%' OR Roles LIKE '%{escaped}%')");
        }

        ReportData.RowFilter = filters.Count > 0 ? string.Join(" AND ", filters) : "";
        UpdateSummaryCounts();
    }

    private void UpdateSummaryCounts()
    {
        if (ReportData == null) return;

        int total = ReportData.Count;
        int ok = 0;
        foreach (DataRowView row in ReportData)
        {
            var flag = row["Flag"]?.ToString() ?? "";
            if (flag is "OK" or "No expiry") ok++;
        }
        TotalMembers = total;
        OkCount = ok;
        AttentionCount = total - ok;
    }

    internal async Task RunReportDirectAsync(CancellationToken ct = default) => await RunReportAsync(ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RunReportAsync(CancellationToken ct)
    {
        if (!_main.IsAuthenticated)
        {
            StatusText = "Please authenticate first.";
            return;
        }

        IsRunning = true;
        ReportData = null;
        var progress = new Progress<string>(msg => ProgressText = msg);

        try
        {
            // Steps 1-3: Units/Teams/Members (cached)
            StatusText = _cache.IsLoaded ? "Using cached member data..." : "Fetching member data...";
            await _cache.EnsureLoadedAsync(progress, ct);
            var members = _cache.Members!;

            StatusText = "Step 4: Fetching training records...";
            await _auth.RefreshTokenAsync();
            var training = await _trainingService.FetchAllTrainingAsync(members, progress);
            ct.ThrowIfCancellationRequested();

            StatusText = "Step 5: Building report...";
            var (report, sortedTitles) = TrainingService.BuildReport(members, training);
            _reportRows = report;
            _sortedTitles = sortedTitles;

            // Build DataTable for dynamic columns
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Total Trainings", typeof(int));
            var lastExpiring = sortedTitles.Where(ExpiringTrainings.Contains).LastOrDefault();
            foreach (var title in sortedTitles)
            {
                dt.Columns.Add(title, typeof(string));
                if (ExpiringTrainings.Contains(title))
                    dt.Columns.Add($"{title} Warning", typeof(string));
                // Insert Flag right after the last expiring training
                if (title == lastExpiring)
                    dt.Columns.Add("Flag", typeof(string));
            }
            dt.Columns.Add("Roles", typeof(string));

            foreach (var row in report)
            {
                var dr = dt.NewRow();
                dr["Name"] = row.Name;
                dr["Total Trainings"] = row.TotalTrainings;
                foreach (var title in sortedTitles)
                {
                    dr[title] = row.TrainingColumns.GetValueOrDefault(title, "");
                    if (ExpiringTrainings.Contains(title))
                        dr[$"{title} Warning"] = row.TrainingColumns.GetValueOrDefault($"{title} Warning", "");
                }
                dr["Flag"] = row.Flag;
                dr["Roles"] = row.Roles;
                dt.Rows.Add(dr);
            }

            ReportData = dt.DefaultView;
            ApplySearchFilter();
            StatusText = $"Done - {TotalMembers} members ({OkCount} OK, {AttentionCount} need attention)";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Report generation cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

}
