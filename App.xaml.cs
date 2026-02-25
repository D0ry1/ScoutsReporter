using System.Windows;
using OfficeOpenXml;
using ScoutsReporter.Services;
using Velopack;

namespace ScoutsReporter;

public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        var app = new App();
        app.InitializeComponent();
        app.MainWindow = new MainWindow();
        app.MainWindow.Show();
        app.Run();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        ThemeService.ApplyTheme(SettingsService.LoadIsDarkMode());
        base.OnStartup(e);
    }
}
