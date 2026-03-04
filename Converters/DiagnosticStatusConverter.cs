using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ScoutsReporter.Converters;

public class DiagnosticStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var category = value?.ToString() ?? "";
        return category switch
        {
            "Success" => FlagToColorConverter.FindBrush("StatusOkBrush", Color.FromRgb(0x23, 0xa9, 0x50)),
            "Error" => FlagToColorConverter.FindBrush("StatusDangerBrush", Color.FromRgb(0xe2, 0x2e, 0x12)),
            "Info" => FlagToColorConverter.FindBrush("StatusWarningBrush", Color.FromRgb(0xFF, 0x98, 0x00)),
            _ => FlagToColorConverter.FindBrush("PrimaryTextBrush", Colors.Gray),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
