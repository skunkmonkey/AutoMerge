using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AutoMerge.UI.ViewModels;

namespace AutoMerge.UI.Views.Dialogs;

public sealed partial class PreferencesDialog : Window
{
    public PreferencesDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PreferencesViewModel viewModel)
        {
            if (viewModel.SaveCommand.CanExecute(null))
            {
                await viewModel.SaveCommand.ExecuteAsync(null).ConfigureAwait(true);
            }
        }

        Close(true);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
