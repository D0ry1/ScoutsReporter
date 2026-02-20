using CommunityToolkit.Mvvm.ComponentModel;

namespace ScoutsReporter.Models;

public partial class SelectableUnit : ObservableObject
{
    public UnitInfo Unit { get; }

    [ObservableProperty]
    private bool _isSelected = true;

    public string UnitName => Unit.UnitName;

    public SelectableUnit(UnitInfo unit)
    {
        Unit = unit;
    }
}
