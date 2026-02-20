using System.Globalization;
using System.Windows;
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
            "EXPIRED" => FindBrush("StatusDangerBrush", Color.FromRgb(0xe2, 0x2e, 0x12)),
            "EXPIRING SOON" => new SolidColorBrush(Color.FromRgb(0xff, 0xe6, 0x27)),
            "ACTION NEEDED" => FindBrush("StatusDangerBrush", Color.FromRgb(0xe2, 0x2e, 0x12)),
            "DBS IN PROGRESS" => new SolidColorBrush(Color.FromRgb(0xff, 0xe6, 0x27)),
            "NO DISCLOSURE" => FindBrush("StatusNavyBrush", Color.FromRgb(0x00, 0x39, 0x82)),
            "NOT IN SYSTEM" => FindBrush("StatusNavyBrush", Color.FromRgb(0x00, 0x39, 0x82)),
            "MISSING" => FindBrush("StatusDangerBrush", Color.FromRgb(0xe2, 0x2e, 0x12)),
            "IN PROGRESS" => new SolidColorBrush(Color.FromRgb(0xff, 0xe6, 0x27)),
            "NO PERMITS" => FindBrush("StatusNavyBrush", Color.FromRgb(0x00, 0x39, 0x82)),
            "OK" => FindBrush("StatusOkBrush", Color.FromRgb(0x23, 0xa9, 0x50)),
            "No expiry" => FindBrush("StatusOkBrush", Color.FromRgb(0x23, 0xa9, 0x50)),
            _ => FindBrush("PrimaryTextBrush", Colors.Black),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

    internal static Brush FindBrush(string key, Color fallbackColor)
    {
        if (Application.Current?.TryFindResource(key) is Brush brush)
            return brush;
        return new SolidColorBrush(fallbackColor);
    }
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
        => value is true ? Visibility.Visible : Visibility.Collapsed;

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
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PercentToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var percent = System.Convert.ToDouble(value);
        if (percent >= 90) return FlagToColorConverter.FindBrush("StatusOkBrush", Color.FromRgb(0x23, 0xa9, 0x50));
        if (percent >= 70) return FlagToColorConverter.FindBrush("StatusWarningBrush", Color.FromRgb(0xFF, 0x98, 0x00));
        return FlagToColorConverter.FindBrush("StatusDangerBrush", Color.FromRgb(0xe2, 0x2e, 0x12));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
