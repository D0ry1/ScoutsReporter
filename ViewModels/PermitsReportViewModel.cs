using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Microsoft.Win32;
using ScoutsReporter.Models;
using ScoutsReporter.Services;
using ScoutsReporter.Views;

namespace ScoutsReporter.ViewModels;

public partial class PermitsReportViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly AuthService _auth;
    private readonly PermitService _permitService;
    private readonly DataCacheService _cache;
    private readonly MainViewModel _main;

    [ObservableProperty]
    private string _statusText = "Click 'Run Report' to generate the Permits report.";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _progressText = "";

    public ObservableCollection<PermitReportRow> ReportRows { get; } = new();
    private List<PermitReportRow> _allRows = new();

    [ObservableProperty] private int _totalMembers;
    [ObservableProperty] private int _withPermits;
    [ObservableProperty] private int _attentionCount;

    [ObservableProperty]
    private bool _hideNoPermits;

    [ObservableProperty]
    private string _searchText = "";

    partial void OnHideNoPermitsChanged(bool value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public void ClearReport()
    {
        ReportRows.Clear();
        _allRows.Clear();
        TotalMembers = 0;
        WithPermits = 0;
        AttentionCount = 0;
        HideNoPermits = false;
        SearchText = "";
        StatusText = "Click 'Run Report' to generate the Permits report.";
        ProgressText = "";
    }

    public PermitsReportViewModel(ApiService api, AuthService auth, PermitService permitService, DataCacheService cache, MainViewModel main)
    {
        _api = api;
        _auth = auth;
        _permitService = permitService;
        _cache = cache;
        _main = main;
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

            StatusText = "Step 4: Fetching permit records...";
            await _auth.RefreshTokenAsync();
            var permits = await _permitService.FetchAllPermitsAsync(members, progress);
            ct.ThrowIfCancellationRequested();

            StatusText = "Step 5: Building report...";
            _allRows = PermitService.BuildReport(members, permits);

            TotalMembers = _allRows.Count;
            WithPermits = _allRows.Count(r => r.Flag != "NO PERMITS");
            AttentionCount = _allRows.Count(r => r.Flag is not "OK" and not "NO PERMITS");
            ApplyFilter();
            StatusText = $"Done - {TotalMembers} members ({WithPermits} with permits, {AttentionCount} need attention)";
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

    private void ApplyFilter()
    {
        ReportRows.Clear();
        IEnumerable<PermitReportRow> filtered = _allRows;

        if (HideNoPermits)
            filtered = filtered.Where(r => r.Flag != "NO PERMITS");

        var search = SearchText.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(r =>
                r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Flag.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Roles.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var row in filtered)
            ReportRows.Add(row);
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (ReportRows.Count == 0) return;

        var result = MessageBox.Show(
            "WARNING: CSV files cannot be password-protected.\n\nThis report contains sensitive personal data. The exported CSV file will be completely unencrypted and readable by anyone with access to it.\n\nFor secure exports, use the Excel option instead which allows password protection.\n\nAre you sure you want to export as unprotected CSV?",
            "CSV Export Warning",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var dlg = new SaveFileDialog
        {
            Title = "Export Permits Report",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"permits_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                CsvExportService.ExportPermitsReport(ReportRows.ToList(), dlg.FileName);
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

        var pwDialog = new PasswordDialog { Owner = Application.Current.MainWindow };
        if (pwDialog.ShowDialog() != true) return;

        var dlg = new SaveFileDialog
        {
            Title = "Export Permits Report as Excel",
            Filter = "Excel files (*.xlsx)|*.xlsx",
            FileName = $"permits_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                ExcelExportService.ExportPermitsReport(ReportRows.ToList(), dlg.FileName, pwDialog.Password);
                StatusText = $"Excel exported to {dlg.FileName}";
            }
            catch (Exception ex)
            {
                StatusText = $"Export error: {ex.Message}";
            }
        }
    }
}
