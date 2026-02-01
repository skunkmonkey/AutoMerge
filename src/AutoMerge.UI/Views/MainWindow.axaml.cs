using AutoMerge.Application.Services;
using AutoMerge.UI.Services;
using AutoMerge.UI.ViewModels;
using AutoMerge.UI.Views.Dialogs;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMerge.UI.Views;

public sealed partial class MainWindow : Window
{
    private readonly IServiceProvider _services;

    public MainWindow(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        InitializeComponent();
        RegisterShortcuts();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void RegisterShortcuts()
    {
        var shortcutService = _services.GetRequiredService<KeyboardShortcutService>();
        shortcutService.Register(
            this,
            onAccept: () => ExecuteCommand(vm => vm.AcceptCommand.Execute(null)),
            onCancel: () => ExecuteCommand(vm => vm.CancelCommand.Execute(null)),
            onSaveDraft: () => _ = SaveDraftAsync(),
            onUndo: () => ExecuteCommand(vm => vm.MergedResultViewModel.UndoCommand.Execute(null)),
            onRedo: () => ExecuteCommand(vm => vm.MergedResultViewModel.RedoCommand.Execute(null)),
            onOpenPreferences: () => _ = OpenPreferencesAsync());
    }

    private void ExecuteCommand(Action<MainWindowViewModel> action)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            action(viewModel);
        }
    }

    private async Task SaveDraftAsync()
    {
        var autoSaveService = _services.GetRequiredService<AutoSaveService>();
        await autoSaveService.SaveDraftNow().ConfigureAwait(true);
    }

    private async Task OpenPreferencesAsync()
    {
        var dialogService = _services.GetRequiredService<DialogService>();
        var viewModel = _services.GetRequiredService<PreferencesViewModel>();
        var dialog = new PreferencesDialog { DataContext = viewModel };
        await dialogService.ShowDialogAsync(this, dialog).ConfigureAwait(true);
    }

    private async Task OpenMergeAsync()
    {
        var dialogService = _services.GetRequiredService<DialogService>();
        var viewModel = _services.GetRequiredService<MergeInputDialogViewModel>();
        var dialog = new MergeInputDialog { DataContext = viewModel };
        var input = await dialogService.ShowDialogAsync<AutoMerge.Core.Models.MergeInput>(this, dialog).ConfigureAwait(true);

        if (input is null)
        {
            return;
        }

        if (DataContext is MainWindowViewModel mainViewModel)
        {
            await mainViewModel.InitializeAsync(input).ConfigureAwait(true);
        }
    }

    private async void OnSaveDraftClicked(object? sender, RoutedEventArgs e)
    {
        await SaveDraftAsync().ConfigureAwait(true);
    }

    private async void OnOpenPreferencesClicked(object? sender, RoutedEventArgs e)
    {
        await OpenPreferencesAsync().ConfigureAwait(true);
    }

    private async void OnOpenMergeClicked(object? sender, RoutedEventArgs e)
    {
        await OpenMergeAsync().ConfigureAwait(true);
    }
}
