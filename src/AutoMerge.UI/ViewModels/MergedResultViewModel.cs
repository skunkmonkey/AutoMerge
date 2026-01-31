using AutoMerge.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoMerge.UI.ViewModels;

public sealed partial class MergedResultViewModel : ViewModelBase
{
    private readonly IConflictParser _conflictParser;
    private string _initialContent = string.Empty;
    private string _baseContent = string.Empty;
    private string _localContent = string.Empty;
    private string _remoteContent = string.Empty;

    public MergedResultViewModel(IConflictParser conflictParser)
    {
        _conflictParser = conflictParser;
        UndoCommand = new RelayCommand(() => { });
        RedoCommand = new RelayCommand(() => { });
        RevertToBaseCommand = new RelayCommand(() => Content = _baseContent);
        RevertToLocalCommand = new RelayCommand(() => Content = _localContent);
        RevertToRemoteCommand = new RelayCommand(() => Content = _remoteContent);
    }

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _hasConflictMarkers;

    [ObservableProperty]
    private string _syntaxLanguage = string.Empty;

    public IRelayCommand UndoCommand { get; }
    public IRelayCommand RedoCommand { get; }
    public IRelayCommand RevertToBaseCommand { get; }
    public IRelayCommand RevertToLocalCommand { get; }
    public IRelayCommand RevertToRemoteCommand { get; }

    public void SetSourceContents(string baseContent, string localContent, string remoteContent, string mergedContent)
    {
        _baseContent = baseContent;
        _localContent = localContent;
        _remoteContent = remoteContent;
        _initialContent = mergedContent;
        Content = mergedContent;
        UpdateValidationState();
    }

    partial void OnContentChanged(string value)
    {
        UpdateValidationState();
    }

    private void UpdateValidationState()
    {
        HasConflictMarkers = _conflictParser.HasConflictMarkers(Content);
        IsDirty = !string.Equals(Content, _initialContent, StringComparison.Ordinal);
    }
}
