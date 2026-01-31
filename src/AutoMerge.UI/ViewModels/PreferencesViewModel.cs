using AutoMerge.Application.UseCases.LoadPreferences;
using AutoMerge.Application.UseCases.SavePreferences;
using AutoMerge.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

namespace AutoMerge.UI.ViewModels;

public sealed partial class PreferencesViewModel : ViewModelBase
{
    private readonly LoadPreferencesHandler _loadHandler;
    private readonly SavePreferencesHandler _saveHandler;

    public PreferencesViewModel(LoadPreferencesHandler loadHandler, SavePreferencesHandler saveHandler)
    {
        _loadHandler = loadHandler;
        _saveHandler = saveHandler;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => { });
        ResetToDefaultsCommand = new RelayCommand(ResetToDefaults);

        _ = LoadAsync();
    }

    [ObservableProperty]
    private DefaultBias _defaultBias;

    [ObservableProperty]
    private bool _autoAnalyzeOnLoad;

    [ObservableProperty]
    private Theme _theme;

    public IReadOnlyList<DefaultBias> DefaultBiasOptions { get; } = Enum.GetValues<DefaultBias>();

    public IReadOnlyList<Theme> ThemeOptions { get; } = Enum.GetValues<Theme>();

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand ResetToDefaultsCommand { get; }

    private async Task LoadAsync()
    {
        var preferences = await _loadHandler.ExecuteAsync().ConfigureAwait(false);
        DefaultBias = preferences.DefaultBias;
        AutoAnalyzeOnLoad = preferences.AutoAnalyzeOnLoad;
        Theme = preferences.Theme;
    }

    private async Task SaveAsync()
    {
        var preferences = new UserPreferences(DefaultBias, AutoAnalyzeOnLoad, Theme);
        await _saveHandler.ExecuteAsync(new SavePreferencesCommand(preferences)).ConfigureAwait(false);
    }

    private void ResetToDefaults()
    {
        var defaults = UserPreferences.Default;
        DefaultBias = defaults.DefaultBias;
        AutoAnalyzeOnLoad = defaults.AutoAnalyzeOnLoad;
        Theme = defaults.Theme;
    }
}
