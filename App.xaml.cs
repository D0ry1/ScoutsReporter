using System.Windows;
using OfficeOpenXml;
using ScoutsReporter.Services;

namespace ScoutsReporter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        ThemeService.ApplyTheme(SettingsService.LoadIsDarkMode());
        base.OnStartup(e);
    }
}
