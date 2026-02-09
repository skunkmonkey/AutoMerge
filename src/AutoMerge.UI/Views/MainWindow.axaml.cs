using System.ComponentModel;
using AutoMerge.Logic.Services;
using AutoMerge.Core.Models;
using AutoMerge.UI.Services;
using AutoMerge.UI.ViewModels;
using AutoMerge.UI.Views.Dialogs;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMerge.UI.Views;

public sealed partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    private bool _closingHandled;

    public MainWindow(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        InitializeComponent();
        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://AutoMerge.UI/Assets/AppIcon_32.ico")));
        RegisterShortcuts();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
            }
        };
    }

    private bool _setupDialogShown;

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.ShowAiSetupDialog) &&
            sender is MainWindowViewModel vm && vm.ShowAiSetupDialog)
        {
            vm.ShowAiSetupDialog = false;

            // Only show once per app session to avoid nagging
            if (_setupDialogShown)
                return;
            _setupDialogShown = true;

            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                await ShowCopilotSetupDialogAsync(vm).ConfigureAwait(true);
            });
        }
    }

    /// <summary>
    /// When the user closes the window via the X button (or Alt+F4), treat it
    /// as a cancel so that git/SourceTree sees exit code 1 and does NOT mark
    /// the conflict as resolved. Skip if the session was already explicitly
    /// accepted (Saved) or explicitly cancelled. In diff-only mode, closing
    /// always succeeds (exit code 0) since there is no merge output.
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_closingHandled && DataContext is MainWindowViewModel vm)
        {
            if (vm.IsDiffMode)
            {
                // Diff mode: close gracefully with success
                _closingHandled = true;
                vm.CloseDiffCommand.Execute(null);
            }
            else if (vm.IsSessionLoaded &&
                     vm.State != SessionState.Saved && vm.State != SessionState.Cancelled)
            {
                _closingHandled = true;
                vm.CancelCommand.Execute(null);
            }
        }

        base.OnClosing(e);
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

    private async Task ShowCopilotSetupDialogAsync(MainWindowViewModel viewModel)
    {
        var dialog = new CopilotSetupDialog
        {
            ErrorMessage = viewModel.AiStatusMessage
        };

        var dialogService = _services.GetRequiredService<DialogService>();
        await dialogService.ShowDialogAsync(this, dialog).ConfigureAwait(true);

        if (dialog.RetryRequested)
        {
            _setupDialogShown = false; // Allow dialog to reappear if retry also fails
            if (viewModel.ReconnectAiCommand.CanExecute(null))
            {
                await viewModel.ReconnectAiCommand.ExecuteAsync(null).ConfigureAwait(true);
            }
        }
    }
}
