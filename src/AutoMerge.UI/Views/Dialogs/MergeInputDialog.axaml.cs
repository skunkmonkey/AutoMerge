using System.Linq;
using System.Threading.Tasks;
using AutoMerge.UI.Localization;
using AutoMerge.UI.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

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
        var path = await ShowOpenFileAsync(UIStrings.MergeInputDialogSelectBaseFileTitle).ConfigureAwait(true);
        if (path is not null && DataContext is MergeInputDialogViewModel viewModel)
        {
            viewModel.BasePath = path;
        }
    }

    private async void OnBrowseLocalClicked(object? sender, RoutedEventArgs e)
    {
        var path = await ShowOpenFileAsync(UIStrings.MergeInputDialogSelectLocalFileTitle).ConfigureAwait(true);
        if (path is not null && DataContext is MergeInputDialogViewModel viewModel)
        {
            viewModel.LocalPath = path;
        }
    }

    private async void OnBrowseRemoteClicked(object? sender, RoutedEventArgs e)
    {
        var path = await ShowOpenFileAsync(UIStrings.MergeInputDialogSelectRemoteFileTitle).ConfigureAwait(true);
        if (path is not null && DataContext is MergeInputDialogViewModel viewModel)
        {
            viewModel.RemotePath = path;
        }
    }

    private async void OnBrowseMergedClicked(object? sender, RoutedEventArgs e)
    {
        var path = await ShowSaveFileAsync(UIStrings.MergeInputDialogSelectMergedFileTitle).ConfigureAwait(true);
        if (path is not null && DataContext is MergeInputDialogViewModel viewModel)
        {
            viewModel.MergedPath = path;
        }
    }

    private async Task<string?> ShowOpenFileAsync(string title)
    {
        var provider = StorageProvider;
        if (provider is null)
        {
            return null;
        }

        var result = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        }).ConfigureAwait(true);

        return result.FirstOrDefault()?.Path.LocalPath;
    }

    private async Task<string?> ShowSaveFileAsync(string title)
    {
        var provider = StorageProvider;
        if (provider is null)
        {
            return null;
        }

        var result = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title
        }).ConfigureAwait(true);

        return result?.Path.LocalPath;
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
