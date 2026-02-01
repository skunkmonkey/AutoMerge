using System.Linq;
using System.Threading.Tasks;
using AutoMerge.UI.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AutoMerge.UI.Views.Dialogs;

public sealed partial class MergeInputDialog : Window
{
    public MergeInputDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnBrowseBaseClicked(object? sender, RoutedEventArgs e)
    {
        var path = await ShowOpenFileAsync("Select base file").ConfigureAwait(true);
        if (path is not null && DataContext is MergeInputDialogViewModel viewModel)
        {
            viewModel.BasePath = path;
        }
    }

    private async void OnBrowseLocalClicked(object? sender, RoutedEventArgs e)
    {
        var path = await ShowOpenFileAsync("Select local (ours) file").ConfigureAwait(true);
        if (path is not null && DataContext is MergeInputDialogViewModel viewModel)
        {
            viewModel.LocalPath = path;
        }
    }

    private async void OnBrowseRemoteClicked(object? sender, RoutedEventArgs e)
    {
        var path = await ShowOpenFileAsync("Select remote (theirs) file").ConfigureAwait(true);
        if (path is not null && DataContext is MergeInputDialogViewModel viewModel)
        {
            viewModel.RemotePath = path;
        }
    }

    private async void OnBrowseMergedClicked(object? sender, RoutedEventArgs e)
    {
        var path = await ShowSaveFileAsync("Select merged output file").ConfigureAwait(true);
        if (path is not null && DataContext is MergeInputDialogViewModel viewModel)
        {
            viewModel.MergedPath = path;
        }
    }

    private async Task<string?> ShowOpenFileAsync(string title)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            AllowMultiple = false
        };

        var result = await dialog.ShowAsync(this).ConfigureAwait(true);
        return result?.FirstOrDefault();
    }

    private async Task<string?> ShowSaveFileAsync(string title)
    {
        var dialog = new SaveFileDialog
        {
            Title = title
        };

        return await dialog.ShowAsync(this).ConfigureAwait(true);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnOpenClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MergeInputDialogViewModel viewModel)
        {
            Close(null);
            return;
        }

        if (viewModel.TryBuildMergeInput(out var mergeInput, out var errorMessage))
        {
            Close(mergeInput);
            return;
        }

        viewModel.ErrorMessage = errorMessage;
    }
}
