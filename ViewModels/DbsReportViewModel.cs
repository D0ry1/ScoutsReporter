using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ScoutsReporter.Models;
using ScoutsReporter.Services;

namespace ScoutsReporter.ViewModels;

public partial class DbsReportViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly AuthService _auth;
    private readonly DisclosureService _disclosureService;
    private readonly DataCacheService _cache;
    private readonly MainViewModel _main;

    [ObservableProperty]
    private string _statusText = "Click 'Run Report' to generate the DBS report.";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _progressText = "";

    public ObservableCollection<DbsReportRow> ReportRows { get; } = new();

    // Summary counts
    [ObservableProperty] private int _totalMembers;
    [ObservableProperty] private int _okCount;
    [ObservableProperty] private int _attentionCount;

    [ObservableProperty]
    private string _searchText = "";

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public DbsReportViewModel(ApiService api, AuthService auth, DisclosureService disclosureService, DataCacheService cache, MainViewModel main)
    {
        _api = api;
        _auth = auth;
        _disclosureService = disclosureService;
        _cache = cache;
        _main = main;
    }

    private List<DbsReportRow> _allRows = new();

    public void ClearReport()
    {
        ReportRows.Clear();
        _allRows.Clear();
        TotalMembers = 0;
        OkCount = 0;
        AttentionCount = 0;
        SearchText = "";
        StatusText = "Click 'Run Report' to generate the DBS report.";
        ProgressText = "";
    }

    private void ApplyFilter()
    {
        ReportRows.Clear();
        var search = SearchText.Trim();
        var filtered = string.IsNullOrEmpty(search)
            ? _allRows
            : _allRows.Where(r =>
                r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Flag.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Roles.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.OnboardingDbs.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.DisclosureStatus.Contains(search, StringComparison.OrdinalIgnoreCase));
        foreach (var row in filtered)
            ReportRows.Add(row);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RunReportAsync(CancellationToken ct)
    {
        if (!_main.IsAuthenticated)
        {
            StatusText = "Please authenticate first.";
            return;
        }

        IsRunning = true;
        ReportRows.Clear();
        var progress = new Progress<string>(msg => ProgressText = msg);

        try
        {
            // Steps 1-3: Units/Teams/Members (cached)
            StatusText = _cache.IsLoaded ? "Using cached member data..." : "Fetching member data...";
            await _cache.EnsureLoadedAsync(progress, ct);
            var members = _cache.Members!;

            // Step 4: Onboarding
            StatusText = "Step 4: Fetching onboarding actions...";
            await _auth.RefreshTokenAsync();
            var (onboarding, _) = await _api.FetchOnboardingActionsAsync();
            DisclosureService.LinkMembersToOnboarding(members, onboarding);
            ct.ThrowIfCancellationRequested();

            // Step 5: Disclosures
            StatusText = "Step 5: Fetching disclosure records...";
            var disclosures = await _disclosureService.FetchAllDisclosuresAsync(members, progress);
            ct.ThrowIfCancellationRequested();

            // Step 6: Build report
            StatusText = "Step 6: Building report...";
            _allRows = DisclosureService.BuildReport(members, onboarding, disclosures);

            SearchText = "";
            ApplyFilter();

            TotalMembers = _allRows.Count;
            OkCount = _allRows.Count(r => r.Flag == "OK");
            AttentionCount = _allRows.Count(r => r.Flag != "OK");
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

    [RelayCommand]
    private void ExportCsv()
    {
        if (ReportRows.Count == 0) return;

        var dlg = new SaveFileDialog
        {
            Title = "Export DBS Report",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"dbs_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                CsvExportService.ExportDbsReport(ReportRows.ToList(), dlg.FileName);
                StatusText = $"CSV exported to {dlg.FileName}";
            }
            catch (Exception ex)
            {
                StatusText = $"Export error: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (ReportRows.Count == 0) return;

        var dlg = new SaveFileDialog
        {
            Title = "Export DBS Report as Excel",
            Filter = "Excel files (*.xlsx)|*.xlsx",
            FileName = $"dbs_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                ExcelExportService.ExportDbsReport(ReportRows.ToList(), dlg.FileName);
                StatusText = $"Excel exported to {dlg.FileName}";
            }
            catch (Exception ex)
            {
                StatusText = $"Export error: {ex.Message}";
            }
        }
    }
}
