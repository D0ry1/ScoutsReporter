using System.Windows;

namespace ScoutsReporter.Services;

public static class ThemeService
{
    private static readonly Uri LightThemeUri = new("Themes/LightTheme.xaml", UriKind.Relative);
    private static readonly Uri DarkThemeUri = new("Themes/DarkTheme.xaml", UriKind.Relative);

    public static void ApplyTheme(bool isDark)
    {
        var app = Application.Current;
        if (app == null) return;

        var mergedDicts = app.Resources.MergedDictionaries;

        // Remove existing theme dictionary (always the first one)
        if (mergedDicts.Count > 0)
            mergedDicts.RemoveAt(0);

        var themeUri = isDark ? DarkThemeUri : LightThemeUri;
        var themeDict = new ResourceDictionary { Source = themeUri };
        mergedDicts.Insert(0, themeDict);
    }
}
