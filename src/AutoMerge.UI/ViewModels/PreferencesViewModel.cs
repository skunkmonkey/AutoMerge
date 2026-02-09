using AutoMerge.Logic.UseCases.LoadAiModelOptions;
using AutoMerge.Logic.UseCases.LoadPreferences;
using AutoMerge.Logic.UseCases.SavePreferences;
using AutoMerge.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

namespace AutoMerge.UI.ViewModels;

public sealed partial class PreferencesViewModel : ViewModelBase
{
    private readonly LoadAiModelOptionsHandler _loadAiModelOptionsHandler;
    private readonly LoadPreferencesHandler _loadHandler;
    private readonly SavePreferencesHandler _saveHandler;

    public PreferencesViewModel(
        LoadAiModelOptionsHandler loadAiModelOptionsHandler,
        LoadPreferencesHandler loadHandler,
        SavePreferencesHandler saveHandler)
    {
        _loadAiModelOptionsHandler = loadAiModelOptionsHandler;
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

    [ObservableProperty]
    private string _aiModel = UserPreferences.Default.AiModel;

    [ObservableProperty]
    private IReadOnlyList<string> _aiModelOptions = Array.Empty<string>();

    public IReadOnlyList<DefaultBias> DefaultBiasOptions { get; } = Enum.GetValues<DefaultBias>();

    public IReadOnlyList<Theme> ThemeOptions { get; } = Enum.GetValues<Theme>();

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand ResetToDefaultsCommand { get; }

    private async Task LoadAsync()
    {
        AiModelOptions = await _loadAiModelOptionsHandler.ExecuteAsync().ConfigureAwait(false);
        var preferences = await _loadHandler.ExecuteAsync().ConfigureAwait(false);
        DefaultBias = preferences.DefaultBias;
        AutoAnalyzeOnLoad = preferences.AutoAnalyzeOnLoad;
        Theme = preferences.Theme;
        AiModel = preferences.AiModel;
    }

    private async Task SaveAsync()
    {
        var preferences = new UserPreferences(DefaultBias, AutoAnalyzeOnLoad, Theme, AiModel);
        await _saveHandler.ExecuteAsync(new SavePreferencesCommand(preferences)).ConfigureAwait(false);
    }

    private void ResetToDefaults()
    {
        var defaults = UserPreferences.Default;
        DefaultBias = defaults.DefaultBias;
        AutoAnalyzeOnLoad = defaults.AutoAnalyzeOnLoad;
        Theme = defaults.Theme;
        AiModel = defaults.AiModel;
    }
}
