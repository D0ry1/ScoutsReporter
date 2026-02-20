using System.Windows;
using OfficeOpenXml;

namespace ScoutsReporter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        base.OnStartup(e);
    }
}
