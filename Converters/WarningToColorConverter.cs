using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media;

namespace ScoutsReporter.Converters;

public partial class WarningToColorConverter : IValueConverter
{
    [GeneratedRegex(@"(\d+)\s+days?\s+remaining", RegexOptions.IgnoreCase)]
    private static partial Regex DaysRemainingRegex();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return FlagToColorConverter.FindBrush("PrimaryTextBrush", Colors.Black);

        // EXPIRED / Expired
        if (text.Contains("EXPIRED", StringComparison.OrdinalIgnoreCase)
            && !text.Contains("EXPIRING", StringComparison.OrdinalIgnoreCase))
            return FlagToColorConverter.FindBrush("StatusDangerBrush", Color.FromRgb(0xe2, 0x2e, 0x12));

        // EXPIRING SOON
        if (text.Contains("EXPIRING", StringComparison.OrdinalIgnoreCase))
            return FlagToColorConverter.FindBrush("StatusWarningBrush", Color.FromRgb(0xFF, 0x98, 0x00));

        // X days remaining where X < 60
        var match = DaysRemainingRegex().Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var days) && days < 60)
            return FlagToColorConverter.FindBrush("StatusWarningBrush", Color.FromRgb(0xFF, 0x98, 0x00));

        return FlagToColorConverter.FindBrush("PrimaryTextBrush", Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public partial class WarningToBackgroundConverter : IValueConverter
{
    [GeneratedRegex(@"(\d+)\s+days?\s+remaining", RegexOptions.IgnoreCase)]
    private static partial Regex DaysRemainingRegex();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return new SolidColorBrush(Colors.Transparent);

        if (text.Contains("EXPIRED", StringComparison.OrdinalIgnoreCase)
            && !text.Contains("EXPIRING", StringComparison.OrdinalIgnoreCase))
            return new SolidColorBrush(Color.FromArgb(30, 0xe2, 0x2e, 0x12));

        if (text.Contains("EXPIRING", StringComparison.OrdinalIgnoreCase))
            return new SolidColorBrush(Color.FromArgb(30, 0xFF, 0x98, 0x00));

        var match = DaysRemainingRegex().Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var days) && days < 60)
            return new SolidColorBrush(Color.FromArgb(30, 0xFF, 0x98, 0x00));

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
