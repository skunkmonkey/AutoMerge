using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
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
    private IReadOnlyList<ConflictRegion> _conflictRegions = Array.Empty<ConflictRegion>();

    public MergedResultViewModel(IConflictParser conflictParser)
    {
        _conflictParser = conflictParser;
        UndoCommand = new RelayCommand(() => { });
        RedoCommand = new RelayCommand(() => { });
        RevertToBaseCommand = new RelayCommand(() => Content = _baseContent);
        RevertToLocalCommand = new RelayCommand(() => Content = _localContent);
        RevertToRemoteCommand = new RelayCommand(() => Content = _remoteContent);
        NextConflictCommand = new RelayCommand(GoToNextConflict, () => CanGoNextConflict);
        PreviousConflictCommand = new RelayCommand(GoToPreviousConflict, () => CanGoPreviousConflict);
    }

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _hasConflictMarkers;

    [ObservableProperty]
    private string _syntaxLanguage = string.Empty;

    [ObservableProperty]
    private int _currentConflictIndex;

    [ObservableProperty]
    private int _totalConflictCount;

    [ObservableProperty]
    private string _currentConflictDisplay = "0 / 0";

    /// <summary>
    /// The line number to scroll to when navigating conflicts.
    /// The view subscribes to changes on this property.
    /// </summary>
    [ObservableProperty]
    private int _scrollToLine;

    public bool CanGoNextConflict => TotalConflictCount > 0 && CurrentConflictIndex < TotalConflictCount;
    public bool CanGoPreviousConflict => TotalConflictCount > 0 && CurrentConflictIndex > 1;

    public IRelayCommand UndoCommand { get; }
    public IRelayCommand RedoCommand { get; }
    public IRelayCommand RevertToBaseCommand { get; }
    public IRelayCommand RevertToLocalCommand { get; }
    public IRelayCommand RevertToRemoteCommand { get; }
    public IRelayCommand NextConflictCommand { get; }
    public IRelayCommand PreviousConflictCommand { get; }

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

        // Update conflict regions for navigation
        _conflictRegions = _conflictParser.Parse(Content);
        TotalConflictCount = _conflictRegions.Count;

        // Reset index if conflicts changed
        if (TotalConflictCount == 0)
        {
            CurrentConflictIndex = 0;
        }
        else if (CurrentConflictIndex == 0 && TotalConflictCount > 0)
        {
            CurrentConflictIndex = 1;
        }
        else if (CurrentConflictIndex > TotalConflictCount)
        {
            CurrentConflictIndex = TotalConflictCount;
        }

        UpdateConflictDisplay();
        NextConflictCommand.NotifyCanExecuteChanged();
        PreviousConflictCommand.NotifyCanExecuteChanged();
    }

    private void GoToNextConflict()
    {
        if (CurrentConflictIndex < TotalConflictCount)
        {
            CurrentConflictIndex++;
            UpdateConflictDisplay();
            ScrollToCurrentConflict();
        }
    }

    private void GoToPreviousConflict()
    {
        if (CurrentConflictIndex > 1)
        {
            CurrentConflictIndex--;
            UpdateConflictDisplay();
            ScrollToCurrentConflict();
        }
    }

    private void ScrollToCurrentConflict()
    {
        if (CurrentConflictIndex >= 1 && CurrentConflictIndex <= _conflictRegions.Count)
        {
            var region = _conflictRegions[CurrentConflictIndex - 1];
            // Set to 0 first to ensure PropertyChanged fires even if same line
            ScrollToLine = 0;
            ScrollToLine = region.StartLine;
        }
        NextConflictCommand.NotifyCanExecuteChanged();
        PreviousConflictCommand.NotifyCanExecuteChanged();
    }

    private void UpdateConflictDisplay()
    {
        CurrentConflictDisplay = TotalConflictCount > 0
            ? $"{CurrentConflictIndex} / {TotalConflictCount}"
            : "0 / 0";
    }
}
