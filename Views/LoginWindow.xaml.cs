using System.IO;
using System.Web;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using ScoutsReporter.Services;

namespace ScoutsReporter.Views;

public partial class LoginWindow : Window
{
    private const string RedirectHost = "membership.scouts.org.uk";

    public string? AuthCode { get; private set; }
    public string CodeVerifier { get; }

    public LoginWindow()
    {
        InitializeComponent();

        var (url, verifier) = AuthService.BuildAuthorizeUrl();
        _authorizeUrl = url;
        CodeVerifier = verifier;

        Loaded += OnLoaded;
    }

    private readonly string _authorizeUrl;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScoutsReporter", "WebView2");
        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await LoginBrowser.EnsureCoreWebView2Async(env);
        LoginBrowser.CoreWebView2.NavigationStarting += OnNavigationStarting;
        LoginBrowser.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
        LoginBrowser.CoreWebView2.Navigate(_authorizeUrl);
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // Show the browser once the first page has loaded
        LoadingText.Visibility = Visibility.Collapsed;
        LoginBrowser.Visibility = Visibility.Visible;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
            return;

        if (!uri.Host.Equals(RedirectHost, StringComparison.OrdinalIgnoreCase))
            return;

        var query = HttpUtility.ParseQueryString(uri.Query);
        var code = query["code"];
        if (string.IsNullOrEmpty(code))
            return;

        // We have the auth code - cancel the navigation and close
        e.Cancel = true;
        AuthCode = code;
        DialogResult = true;
        Close();
    }
}
