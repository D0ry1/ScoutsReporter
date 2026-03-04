using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using ScoutsReporter.ViewModels;

namespace ScoutsReporter;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;
        vm.Diagnostics.Entries.CollectionChanged += DiagnosticEntries_CollectionChanged;
    }

    private void DiagnosticEntries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            DiagnosticScrollViewer?.ScrollToEnd();
    }
}
