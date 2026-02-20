using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ScoutsReporter.Views;

public partial class PasswordDialog : Window
{
    private static Brush MetBrush => FindBrush("StatusOkBrush", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
    private static Brush UnmetBrush => FindBrush("PlaceholderTextBrush", new SolidColorBrush(Color.FromRgb(102, 102, 102)));

    public string? Password { get; private set; }

    public PasswordDialog()
    {
        InitializeComponent();
        PasswordInput.Focus();
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        UpdateRuleIndicators();
    }

    private void UpdateRuleIndicators()
    {
        var pw = PasswordInput.Password ?? "";
        RuleLength.Foreground = pw.Length >= 8 ? MetBrush : UnmetBrush;
        RuleUpper.Foreground = pw.Any(char.IsUpper) ? MetBrush : UnmetBrush;
        RuleLower.Foreground = pw.Any(char.IsLower) ? MetBrush : UnmetBrush;
        RuleDigit.Foreground = pw.Any(char.IsDigit) ? MetBrush : UnmetBrush;
    }

    private void OnOkClicked(object sender, RoutedEventArgs e)
    {
        var pw = PasswordInput.Password;

        if (string.IsNullOrEmpty(pw))
        {
            ErrorText.Text = "Password cannot be empty.";
            PasswordInput.Focus();
            return;
        }

        if (pw.Length < 8)
        {
            ErrorText.Text = "Password must be at least 8 characters.";
            PasswordInput.Focus();
            return;
        }

        if (!pw.Any(char.IsUpper))
        {
            ErrorText.Text = "Password must contain an uppercase letter.";
            PasswordInput.Focus();
            return;
        }

        if (!pw.Any(char.IsLower))
        {
            ErrorText.Text = "Password must contain a lowercase letter.";
            PasswordInput.Focus();
            return;
        }

        if (!pw.Any(char.IsDigit))
        {
            ErrorText.Text = "Password must contain a number.";
            PasswordInput.Focus();
            return;
        }

        if (pw != ConfirmInput.Password)
        {
            ErrorText.Text = "Passwords do not match.";
            ConfirmInput.Clear();
            ConfirmInput.Focus();
            return;
        }

        Password = pw;
        DialogResult = true;
        Close();
    }

    private static Brush FindBrush(string key, Brush fallback)
    {
        if (Application.Current?.TryFindResource(key) is Brush brush)
            return brush;
        return fallback;
    }
}
