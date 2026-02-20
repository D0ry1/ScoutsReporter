using System.Windows;
using ScoutsReporter.ViewModels;

namespace ScoutsReporter;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
