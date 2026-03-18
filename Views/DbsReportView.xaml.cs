using System.Windows;
using System.Windows.Controls;
using ScoutsReporter.Services;

namespace ScoutsReporter.Views;

public partial class DbsReportView : UserControl
{
    private const string ReportKey = "Dbs";
    private bool _layoutRestored;

    public DbsReportView()
    {
        InitializeComponent();
        Loaded += DbsReportView_Loaded;
    }

    private void DbsReportView_Loaded(object sender, RoutedEventArgs e)
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

        // Reset sort
        ReportGrid.Items.SortDescriptions.Clear();
        foreach (var col in ReportGrid.Columns)
            col.SortDirection = null;

        // Reset display indices to natural order
        for (int i = 0; i < ReportGrid.Columns.Count; i++)
            ReportGrid.Columns[i].DisplayIndex = i;

        // Reset widths to defaults
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
        ["Mem #"] = 100,
        ["Issued"] = 90,
        ["Expiry"] = 90,
        ["Warning"] = 150,
        ["Onboarding DBS"] = 110,
        ["Disclosure Status"] = 140,
        ["Certificate"] = 110,
        ["Type"] = 100,
        ["Authority"] = 100,
        ["Total"] = 50,
        ["Flag"] = 130,
        ["Outstanding"] = 160,
        ["Roles"] = 300
    };
}
