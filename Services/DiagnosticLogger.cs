using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace ScoutsReporter.Services;

public record DiagnosticEntry(
    DateTime Timestamp,
    string Method,
    string SanitizedUrl,
    int? StatusCode,
    long DurationMs,
    long? ResponseSize,
    string? ErrorMessage);

public partial class DiagnosticLogger
{
    private const int MaxEntries = 500;

    private readonly ConcurrentQueue<DiagnosticEntry> _entries = new();
    private volatile bool _isEnabled;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public event Action<DiagnosticEntry>? EntryAdded;

    public void Log(DiagnosticEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries)
            _entries.TryDequeue(out _);
        EntryAdded?.Invoke(entry);
    }

    public void Clear() => _entries.Clear();

    public IReadOnlyList<DiagnosticEntry> GetEntries() => _entries.ToArray();

    public string FormatForClipboard()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Scouts Reporter API Diagnostics ===");
        sb.AppendLine($"Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Entries: {_entries.Count}");
        sb.AppendLine();

        foreach (var e in _entries)
        {
            sb.Append($"[{e.Timestamp:HH:mm:ss.fff}] {e.Method} {e.SanitizedUrl}");
            if (e.StatusCode.HasValue)
                sb.Append($" -> {e.StatusCode}");
            sb.Append($" ({e.DurationMs}ms)");
            if (e.ResponseSize.HasValue)
                sb.Append($" [{e.ResponseSize}B]");
            if (e.ErrorMessage != null)
                sb.Append($" ERROR: {e.ErrorMessage}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string SanitizeUrl(string url)
    {
        // Replace GUIDs with [id]
        var sanitized = GuidPattern().Replace(url, "[id]");

        // Replace SAS token query strings on Azure table storage URLs
        if (sanitized.Contains(".table.core.windows.net") ||
            sanitized.Contains(".blob.core.windows.net"))
        {
            var qIndex = sanitized.IndexOf('?');
            if (qIndex >= 0)
                sanitized = sanitized[..qIndex] + "?[sas-token]";
        }

        return sanitized;
    }

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidPattern();
}
