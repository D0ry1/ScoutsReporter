using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScoutsReporter.Models;

namespace ScoutsReporter.Services;

public static class ColumnLayoutService
{
    public static void SaveLayout(string reportKey, DataGrid grid)
    {
        var settings = new ColumnLayoutSettings();

        foreach (var col in grid.Columns)
        {
            var header = col.Header?.ToString();
            if (string.IsNullOrEmpty(header)) continue;

            settings.Columns.Add(new ColumnInfo
            {
                Header = header,
                DisplayIndex = col.DisplayIndex,
                Width = col.ActualWidth
            });
        }

        foreach (var sd in grid.Items.SortDescriptions)
        {
            settings.SortDescriptions.Add(new SortInfo
            {
                PropertyName = sd.PropertyName,
                Direction = sd.Direction
            });
        }

        SettingsService.SaveObject($"columnLayout_{reportKey}", settings);
    }

    public static void RestoreLayout(string reportKey, DataGrid grid)
    {
        var settings = SettingsService.LoadObject<ColumnLayoutSettings>($"columnLayout_{reportKey}");
        if (settings == null) return;

        // Build lookup of saved columns by header
        var savedByHeader = new Dictionary<string, ColumnInfo>();
        foreach (var ci in settings.Columns)
            savedByHeader[ci.Header] = ci;

        // Restore widths first
        foreach (var col in grid.Columns)
        {
            var header = col.Header?.ToString();
            if (header != null && savedByHeader.TryGetValue(header, out var info))
                col.Width = new DataGridLength(info.Width);
        }

        // Restore display indices — sort saved columns by DisplayIndex to avoid WPF shifting issues
        var ordered = settings.Columns
            .Where(ci => ci.DisplayIndex >= 0)
            .OrderBy(ci => ci.DisplayIndex)
            .ToList();

        foreach (var ci in ordered)
        {
            var col = grid.Columns.FirstOrDefault(c => c.Header?.ToString() == ci.Header);
            if (col != null && ci.DisplayIndex < grid.Columns.Count)
                col.DisplayIndex = ci.DisplayIndex;
        }

        // Restore sort descriptions
        grid.Items.SortDescriptions.Clear();
        foreach (var si in settings.SortDescriptions)
        {
            grid.Items.SortDescriptions.Add(
                new SortDescription(si.PropertyName, si.Direction));
        }

        // Restore sort direction arrows on column headers
        foreach (var col in grid.Columns)
        {
            var sortMember = GetSortMemberPath(col);
            var sd = settings.SortDescriptions.FirstOrDefault(s => s.PropertyName == sortMember);
            col.SortDirection = sd != null ? sd.Direction : null;
        }
    }

    public static void ClearLayout(string reportKey)
    {
        SettingsService.RemoveKey($"columnLayout_{reportKey}");
    }

    public static void HandleSorting(DataGrid grid, DataGridSortingEventArgs e, string reportKey)
    {
        e.Handled = true;

        var sortMember = GetSortMemberPath(e.Column);
        if (string.IsNullOrEmpty(sortMember)) return;

        bool isShiftHeld = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        // Determine next direction: null → Ascending → Descending → removed
        ListSortDirection? nextDirection = e.Column.SortDirection switch
        {
            null => ListSortDirection.Ascending,
            ListSortDirection.Ascending => ListSortDirection.Descending,
            _ => null // Descending → remove
        };

        if (!isShiftHeld)
        {
            // Single-column sort: clear everything
            grid.Items.SortDescriptions.Clear();
            foreach (var col in grid.Columns)
                col.SortDirection = null;
        }
        else
        {
            // Multi-sort: remove existing entry for this column
            for (int i = grid.Items.SortDescriptions.Count - 1; i >= 0; i--)
            {
                if (grid.Items.SortDescriptions[i].PropertyName == sortMember)
                {
                    grid.Items.SortDescriptions.RemoveAt(i);
                    break;
                }
            }
        }

        if (nextDirection.HasValue)
        {
            grid.Items.SortDescriptions.Add(
                new SortDescription(sortMember, nextDirection.Value));
            e.Column.SortDirection = nextDirection.Value;
        }
        else
        {
            e.Column.SortDirection = null;
        }

        grid.Items.Refresh();
        SaveLayout(reportKey, grid);
    }

    private static string GetSortMemberPath(DataGridColumn col)
    {
        if (col is DataGridBoundColumn bc && bc.Binding is System.Windows.Data.Binding b)
            return b.Path.Path;
        if (col is DataGridTemplateColumn tc)
            return tc.SortMemberPath ?? string.Empty;
        return string.Empty;
    }
}
