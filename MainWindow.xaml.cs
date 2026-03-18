using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScoutsReporter.ViewModels;
using ScoutsReporter.Views;

namespace ScoutsReporter;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;
        vm.Diagnostics.Entries.CollectionChanged += DiagnosticEntries_CollectionChanged;
        Closing += MainWindow_Closing;
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Help,
            (s, e) => new HelpWindow { Owner = this }.ShowDialog()));
    }

    private void DiagnosticEntries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            DiagnosticScrollViewer?.ScrollToEnd();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Save column layouts for all report views before exit
        SaveReportLayouts(this);
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        new HelpWindow { Owner = this }.ShowDialog();
    }

    private static void SaveReportLayouts(DependencyObject parent)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            switch (child)
            {
                case DbsReportView dbs:
                    dbs.SaveCurrentLayout();
                    continue;
                case PermitsReportView permits:
                    permits.SaveCurrentLayout();
                    continue;
                case TrainingReportView training:
                    training.SaveCurrentLayout();
                    continue;
            }
            SaveReportLayouts(child);
        }
    }
}
