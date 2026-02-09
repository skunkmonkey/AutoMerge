using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AutoMerge.UI.Views.Dialogs;

public sealed partial class CopilotSetupDialog : Window
{
    /// <summary>
    /// Set to true when the user clicks "Retry Connection" so the caller can
    /// distinguish between retry and dismiss.
    /// </summary>
    public bool RetryRequested { get; private set; }

    /// <summary>
    /// Optional error message to display in the dialog.
    /// Bound from <see cref="ErrorMessage"/> property via DataContext.
    /// </summary>
    public string? ErrorMessage
    {
        get => DataContext as string;
        set => DataContext = value;
    }

    public CopilotSetupDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnCopyCommandClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string command)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
            {
                await clipboard.SetTextAsync(command).ConfigureAwait(true);
            }
        }
    }

    private void OnRetryClicked(object? sender, RoutedEventArgs e)
    {
        RetryRequested = true;
        Close();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        RetryRequested = false;
        Close();
    }
}
