using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScoutsReporter.Services;

namespace ScoutsReporter.ViewModels;

public class DiagnosticEntryViewModel
{
    public string Summary { get; }
    public string StatusCategory { get; }

    public DiagnosticEntryViewModel(DiagnosticEntry entry)
    {
        var parts = $"[{entry.Timestamp:HH:mm:ss.fff}] {entry.Method} {entry.SanitizedUrl}";
        if (entry.StatusCode.HasValue)
            parts += $" -> {entry.StatusCode}";
        parts += $" ({entry.DurationMs}ms)";
        if (entry.ResponseSize.HasValue)
            parts += $" [{entry.ResponseSize}B]";
        if (entry.ErrorMessage != null)
            parts += $" ERROR: {entry.ErrorMessage}";

        Summary = parts;

        if (entry.ErrorMessage != null)
            StatusCategory = "Error";
        else if (entry.StatusCode is >= 200 and < 300)
            StatusCategory = "Success";
        else if (entry.StatusCode.HasValue)
            StatusCategory = "Error";
        else
            StatusCategory = "Info";
    }
}

public partial class DiagnosticPanelViewModel : ObservableObject
{
    private readonly DiagnosticLogger _logger;

    public ObservableCollection<DiagnosticEntryViewModel> Entries { get; } = new();

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isPanelVisible;

    [ObservableProperty]
    private int _entryCount;

    public DiagnosticPanelViewModel(DiagnosticLogger logger)
    {
        _logger = logger;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _logger.IsEnabled = value;
        IsPanelVisible = value;

        if (value)
        {
            _logger.EntryAdded += OnEntryAdded;
        }
        else
        {
            _logger.EntryAdded -= OnEntryAdded;
            _logger.Clear();
            Entries.Clear();
            EntryCount = 0;
        }
    }

    private void OnEntryAdded(DiagnosticEntry entry)
    {
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            Entries.Add(new DiagnosticEntryViewModel(entry));
            while (Entries.Count > 500)
                Entries.RemoveAt(0);
            EntryCount = Entries.Count;
        });
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        var text = _logger.FormatForClipboard();
        Clipboard.SetText(text);
    }

    [RelayCommand]
    private void ClearEntries()
    {
        _logger.Clear();
        Entries.Clear();
        EntryCount = 0;
    }
}
