using System.Collections.ObjectModel;
using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Microsoft.Win32;
using ScoutsReporter.Models;
using ScoutsReporter.Services;
using ScoutsReporter.Views;

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

    // Keep for CSV export
    private List<TrainingReportRow> _reportRows = new();

    public IReadOnlyList<TrainingReportRow> ReportRowsReadOnly => _reportRows;
    private List<string> _sortedTitles = new();

    [ObservableProperty] private int _totalMembers;
    [ObservableProperty] private int _okCount;
    [ObservableProperty] private int _attentionCount;

    [ObservableProperty]
    private string _searchText = "";

    partial void OnSearchTextChanged(string value) => ApplySearchFilter();

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
        StatusText = "Click 'Run Report' to generate the Training report.";
        ProgressText = "";
    }

    private void ApplySearchFilter()
    {
        if (ReportData == null) return;

        var search = SearchText.Trim();
        if (string.IsNullOrEmpty(search))
        {
            ReportData.RowFilter = "";
            return;
        }

        var escaped = search.Replace("'", "''");
        ReportData.RowFilter = $"Name LIKE '%{escaped}%' OR Flag LIKE '%{escaped}%' OR Roles LIKE '%{escaped}%'";
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
            foreach (var title in sortedTitles)
            {
                dt.Columns.Add(title, typeof(string));
                if (ExpiringTrainings.Contains(title))
                    dt.Columns.Add($"{title} Warning", typeof(string));
            }
            dt.Columns.Add("Flag", typeof(string));
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
            TotalMembers = report.Count;
            OkCount = report.Count(r => r.Flag is "OK" or "No expiry");
            AttentionCount = report.Count(r => r.Flag is not "OK" and not "No expiry");
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
        if (_reportRows.Count == 0) return;

        var result = MessageBox.Show(
            "WARNING: CSV files cannot be password-protected.\n\nThis report contains sensitive personal data. The exported CSV file will be completely unencrypted and readable by anyone with access to it.\n\nFor secure exports, use the Excel option instead which allows password protection.\n\nAre you sure you want to export as unprotected CSV?",
            "CSV Export Warning",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var dlg = new SaveFileDialog
        {
            Title = "Export Training Report",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"training_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                CsvExportService.ExportTrainingReport(_reportRows, _sortedTitles, dlg.FileName);
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
        if (_reportRows.Count == 0) return;

        var pwDialog = new PasswordDialog { Owner = Application.Current.MainWindow };
        if (pwDialog.ShowDialog() != true) return;

        var dlg = new SaveFileDialog
        {
            Title = "Export Training Report as Excel",
            Filter = "Excel files (*.xlsx)|*.xlsx",
            FileName = $"training_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                ExcelExportService.ExportTrainingReport(_reportRows, _sortedTitles, dlg.FileName, pwDialog.Password);
                StatusText = $"Excel exported to {dlg.FileName}";
            }
            catch (Exception ex)
            {
                StatusText = $"Export error: {ex.Message}";
            }
        }
    }
}
