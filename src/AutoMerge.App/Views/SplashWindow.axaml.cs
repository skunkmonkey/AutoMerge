using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace AutoMerge.App.Views;

public sealed partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        var versionText = this.FindControl<TextBlock>("VersionText");
        if (versionText is not null)
        {
            versionText.Text = $"v{version}";
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Updates the status message displayed on the splash screen.
    /// Safe to call from any thread.
    /// </summary>
    public void SetStatus(string message)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            UpdateStatusText(message);
        }
        else
        {
            Dispatcher.UIThread.Post(() => UpdateStatusText(message));
        }
    }

    private void UpdateStatusText(string message)
    {
        var statusText = this.FindControl<TextBlock>("StatusText");
        if (statusText is not null)
        {
            statusText.Text = message;
        }
    }
}
