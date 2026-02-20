using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ScoutsReporter.Converters;

public class FlagToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var flag = value?.ToString() ?? "";
        return flag switch
        {
            "EXPIRED" => new SolidColorBrush(Color.FromRgb(0xe2, 0x2e, 0x12)),      // Scouts Red
            "EXPIRING SOON" => new SolidColorBrush(Color.FromRgb(0xff, 0xe6, 0x27)), // Scouts Yellow
            "ACTION NEEDED" => new SolidColorBrush(Color.FromRgb(0xe2, 0x2e, 0x12)),
            "DBS IN PROGRESS" => new SolidColorBrush(Color.FromRgb(0xff, 0xe6, 0x27)), // Scouts Yellow
            "NO DISCLOSURE" => new SolidColorBrush(Color.FromRgb(0x00, 0x39, 0x82)), // Scouts Navy
            "NOT IN SYSTEM" => new SolidColorBrush(Color.FromRgb(0x00, 0x39, 0x82)),
            "MISSING" => new SolidColorBrush(Color.FromRgb(0xe2, 0x2e, 0x12)),
            "IN PROGRESS" => new SolidColorBrush(Color.FromRgb(0xff, 0xe6, 0x27)),
            "NO PERMITS" => new SolidColorBrush(Color.FromRgb(0x00, 0x39, 0x82)),
            "OK" => new SolidColorBrush(Color.FromRgb(0x23, 0xa9, 0x50)),            // Scouts Green
            "No expiry" => new SolidColorBrush(Color.FromRgb(0x23, 0xa9, 0x50)),
            _ => new SolidColorBrush(Colors.Black),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FlagToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var flag = value?.ToString() ?? "";
        return flag switch
        {
            "EXPIRED" or "ACTION NEEDED" or "MISSING"
                => new SolidColorBrush(Color.FromArgb(30, 0xe2, 0x2e, 0x12)),
            "EXPIRING SOON"
                => new SolidColorBrush(Color.FromArgb(30, 0xff, 0xe6, 0x27)),
            "DBS IN PROGRESS" or "IN PROGRESS"
                => new SolidColorBrush(Color.FromArgb(20, 0xff, 0xe6, 0x27)),
            "OK" or "No expiry"
                => new SolidColorBrush(Colors.Transparent),
            _ => new SolidColorBrush(Colors.Transparent),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? false : true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? false : true;
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
