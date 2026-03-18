using System.ComponentModel;

namespace ScoutsReporter.Models;

public class ColumnLayoutSettings
{
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<SortInfo> SortDescriptions { get; set; } = new();
}

public class ColumnInfo
{
    public string Header { get; set; } = string.Empty;
    public int DisplayIndex { get; set; }
    public double Width { get; set; }
}

public class SortInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public ListSortDirection Direction { get; set; }
}
