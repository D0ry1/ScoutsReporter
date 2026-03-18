using System.Windows;
using System.Windows.Controls;
using ScoutsReporter.Services;

namespace ScoutsReporter.Views;

public partial class PermitsReportView : UserControl
{
    private const string ReportKey = "Permits";
    private bool _layoutRestored;

    public PermitsReportView()
    {
        InitializeComponent();
        Loaded += PermitsReportView_Loaded;
    }

    private void PermitsReportView_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_layoutRestored && ReportGrid.Columns.Count > 0)
        {
            ColumnLayoutService.RestoreLayout(ReportKey, ReportGrid);
            _layoutRestored = true;
        }
    }

    private void ReportGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        ColumnLayoutService.HandleSorting(ReportGrid, e, ReportKey);
    }

    private void ReportGrid_ColumnReordered(object sender, DataGridColumnEventArgs e)
    {
        ColumnLayoutService.SaveLayout(ReportKey, ReportGrid);
    }

    private void ResetColumns_Click(object sender, RoutedEventArgs e)
    {
        ColumnLayoutService.ClearLayout(ReportKey);

        ReportGrid.Items.SortDescriptions.Clear();
        foreach (var col in ReportGrid.Columns)
            col.SortDirection = null;

        for (int i = 0; i < ReportGrid.Columns.Count; i++)
            ReportGrid.Columns[i].DisplayIndex = i;

        var defaults = GetDefaultWidths();
        foreach (var col in ReportGrid.Columns)
        {
            var header = col.Header?.ToString();
            if (header != null && defaults.TryGetValue(header, out var width))
                col.Width = new DataGridLength(width);
        }
    }

    public void SaveCurrentLayout()
    {
        if (ReportGrid.Columns.Count > 0)
            ColumnLayoutService.SaveLayout(ReportKey, ReportGrid);
    }

    private static Dictionary<string, double> GetDefaultWidths() => new()
    {
        ["Name"] = 160,
        ["Total Permits"] = 90,
        ["Flag"] = 130,
        ["Roles"] = 300
    };
}
