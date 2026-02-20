using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScoutsReporter.Models;
using ScoutsReporter.Services;
using ScoutsReporter.Views;
using System.ComponentModel;

namespace ScoutsReporter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;
    private readonly ApiService _api;
    private readonly DataCacheService _cache;

    public DataCacheService Cache => _cache;

    [ObservableProperty]
    private string _statusText = "Click Login to begin.";

    [ObservableProperty]
    private string _groupName = "";

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _userName = "";

    [ObservableProperty]
    private bool _isRunningAll;

    [ObservableProperty]
    private bool _isDarkMode;

    // ── Update banner ────────────────────────────────────────────

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _updateVersion = "";

    [ObservableProperty]
    private string _updateReleaseUrl = "";

    // ── Unit picker ──────────────────────────────────────────────

    public ObservableCollection<SelectableUnit> AvailableUnits { get; } = new();

    [ObservableProperty]
    private bool _hasMultipleUnits;

    [ObservableProperty]
    private bool _isUnitPickerOpen;

    [ObservableProperty]
    private string _unitSelectionSummary = "";

    public DashboardViewModel Dashboard { get; }
    public DbsReportViewModel DbsReport { get; }
    public TrainingReportViewModel TrainingReport { get; }
    public PermitsReportViewModel PermitsReport { get; }

    public MainViewModel()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        _auth = new AuthService(_http);
        _api = new ApiService(_http, _auth);
        _cache = new DataCacheService(_api, GetSelectedUnits);

        var disclosureService = new DisclosureService(_api, _auth);
        var trainingService = new TrainingService(_api, _auth);
        var permitService = new PermitService(_api, _auth);

        DbsReport = new DbsReportViewModel(_api, _auth, disclosureService, _cache, this);
        TrainingReport = new TrainingReportViewModel(_api, _auth, trainingService, _cache, this);
        PermitsReport = new PermitsReportViewModel(_api, _auth, permitService, _cache, this);
        Dashboard = new DashboardViewModel(DbsReport, TrainingReport, this);

        IsDarkMode = SettingsService.LoadIsDarkMode();

        // Try auto-login from saved token on startup
        _ = TryAutoLoginAsync();

        // Check for updates in the background
        _ = CheckForUpdateAsync();
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        ThemeService.ApplyTheme(value);
        SettingsService.SaveIsDarkMode(value);
    }

    private List<UnitInfo> GetSelectedUnits()
    {
        return AvailableUnits
            .Where(u => u.IsSelected)
            .Select(u => u.Unit)
            .ToList();
    }

    private async Task TryAutoLoginAsync()
    {
        var tokenPath = AuthService.DefaultTokenFilePath;
        if (!File.Exists(tokenPath))
            return;

        try
        {
            StatusText = "Found saved token, logging in...";
            _auth.SetTokenFilePath(tokenPath);
            await _auth.RefreshTokenAsync();
            await CompleteLoginAsync();
        }
        catch
        {
            // Silent refresh failed - user can login manually
            StatusText = "Saved token expired. Please log in.";
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        var loginWindow = new LoginWindow
        {
            Owner = Application.Current.MainWindow,
        };

        if (loginWindow.ShowDialog() != true || string.IsNullOrEmpty(loginWindow.AuthCode))
            return;

        try
        {
            StatusText = "Exchanging login code for tokens...";
            _auth.SetTokenFilePath(AuthService.DefaultTokenFilePath);
            await _auth.ExchangeCodeForTokensAsync(loginWindow.AuthCode, loginWindow.CodeVerifier);
            await CompleteLoginAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Login failed: {ex.Message}";
            IsAuthenticated = false;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        _cache.Invalidate();
        IsAuthenticated = false;
        UserName = "";
        GroupName = "";
        StatusText = "Logged out. Click Login to begin.";

        // Clear unit picker state
        foreach (var u in AvailableUnits)
            u.PropertyChanged -= OnUnitSelectionChanged;
        AvailableUnits.Clear();
        HasMultipleUnits = false;
        IsUnitPickerOpen = false;
        UnitSelectionSummary = "";

        Dashboard.ClearDashboard();
        DbsReport.ClearReport();
        TrainingReport.ClearReport();
        PermitsReport.ClearReport();
    }

    [RelayCommand]
    private async Task RunAllReportsAsync()
    {
        if (!IsAuthenticated) return;

        IsRunningAll = true;
        try
        {
            StatusText = "Running all reports...";

            await DbsReport.RunReportCommand.ExecuteAsync(null);
            await TrainingReport.RunReportCommand.ExecuteAsync(null);
            await PermitsReport.RunReportCommand.ExecuteAsync(null);

            StatusText = "All reports complete.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error running reports: {ex.Message}";
        }
        finally
        {
            IsRunningAll = false;
        }
    }

    private async Task CompleteLoginAsync()
    {
        UserName = _auth.UserName;
        IsAuthenticated = true;
        StatusText = $"Logged in as {_auth.UserName}. Loading units...";

        var units = await _api.FetchUnitsAsync();

        // Populate unit picker
        foreach (var u in AvailableUnits)
            u.PropertyChanged -= OnUnitSelectionChanged;
        AvailableUnits.Clear();

        foreach (var unit in units)
        {
            var su = new SelectableUnit(unit);
            su.PropertyChanged += OnUnitSelectionChanged;
            AvailableUnits.Add(su);
        }

        HasMultipleUnits = units.Count > 1;
        UpdateGroupName();
        UpdateUnitSelectionSummary();
        StatusText = $"Ready - {GroupName} ({units.Count} unit(s))";
    }

    // ── Unit picker commands ─────────────────────────────────────

    [RelayCommand]
    private void ToggleUnitPicker()
    {
        IsUnitPickerOpen = !IsUnitPickerOpen;
    }

    [RelayCommand]
    private void SelectAllUnits()
    {
        foreach (var u in AvailableUnits)
            u.IsSelected = true;
    }

    [RelayCommand]
    private void DeselectAllUnits()
    {
        foreach (var u in AvailableUnits)
            u.IsSelected = false;
    }

    private void OnUnitSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SelectableUnit.IsSelected)) return;

        _cache.Invalidate();
        Dashboard.ClearDashboard();
        DbsReport.ClearReport();
        TrainingReport.ClearReport();
        PermitsReport.ClearReport();

        UpdateGroupName();
        UpdateUnitSelectionSummary();
    }

    // ── Update commands ─────────────────────────────────────────

    [RelayCommand]
    private void OpenUpdatePage()
    {
        if (!string.IsNullOrEmpty(UpdateReleaseUrl))
            Process.Start(new ProcessStartInfo(UpdateReleaseUrl) { UseShellExecute = true });
    }

    [RelayCommand]
    private void DismissUpdate()
    {
        IsUpdateAvailable = false;
        SettingsService.SaveDismissedUpdateVersion(UpdateVersion);
    }

    private async Task CheckForUpdateAsync()
    {
        var info = await UpdateService.CheckForUpdateAsync();
        if (info == null)
            return;

        var dismissed = SettingsService.LoadDismissedUpdateVersion();
        if (dismissed == info.NewVersion)
            return;

        UpdateVersion = info.NewVersion;
        UpdateReleaseUrl = info.ReleaseUrl;
        IsUpdateAvailable = true;
    }

    private void UpdateGroupName()
    {
        var selected = AvailableUnits.Where(u => u.IsSelected).ToList();
        if (AvailableUnits.Count <= 1)
            GroupName = AvailableUnits.Count == 1 ? AvailableUnits[0].UnitName : "Unknown Group";
        else if (selected.Count == 1)
            GroupName = selected[0].UnitName;
        else if (selected.Count == AvailableUnits.Count)
            GroupName = $"All {selected.Count} units";
        else
            GroupName = $"{selected.Count} of {AvailableUnits.Count} units selected";
    }

    private void UpdateUnitSelectionSummary()
    {
        var selected = AvailableUnits.Count(u => u.IsSelected);
        UnitSelectionSummary = $"{selected} of {AvailableUnits.Count} units selected";
    }
}
