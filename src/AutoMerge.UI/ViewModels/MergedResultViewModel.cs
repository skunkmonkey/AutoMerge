using System.Globalization;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.UI.Localization;
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

    /// <summary>
    /// Horizontal scroll offset for synchronized scrolling across panels.
    /// </summary>
    [ObservableProperty]
    private double _scrollOffsetX;

    /// <summary>
    /// Vertical scroll offset for synchronized scrolling across panels.
    /// </summary>
    [ObservableProperty]
    private double _scrollOffsetY;

    /// <summary>
    /// Regions that were automatically resolved by deterministic three-way merge logic.
    /// Used for visual highlighting (green tint) in the editor.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<AutoResolvedRegion> _autoResolvedRegions = Array.Empty<AutoResolvedRegion>();

    /// <summary>
    /// Number of conflicts that were automatically resolved on load.
    /// </summary>
    [ObservableProperty]
    private int _autoResolvedCount;

    /// <summary>
    /// True when at least one conflict was auto-resolved, for UI visibility bindings.
    /// </summary>
    [ObservableProperty]
    private bool _hasAutoResolved;

    /// <summary>
    /// The parsed conflict regions from the current content.
    /// Used by MainWindowViewModel to sync source pane scrolling.
    /// </summary>
    public IReadOnlyList<ConflictRegion> ConflictRegions => _conflictRegions;

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

        // Attempt auto-resolution of trivially resolvable conflicts
        var (resolvedContent, autoResolved) = AttemptAutoResolve(mergedContent);
        AutoResolvedRegions = autoResolved;
        AutoResolvedCount = autoResolved.Count;
        HasAutoResolved = autoResolved.Count > 0;

        Content = resolvedContent;
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
        if (TotalConflictCount > 0)
        {
            CurrentConflictDisplay = AutoResolvedCount > 0
                ? string.Format(
                    CultureInfo.CurrentUICulture,
                    UIStrings.MergedResultConflictDisplayWithAutoResolvedFormat,
                    CurrentConflictIndex,
                    TotalConflictCount,
                    AutoResolvedCount)
                : string.Format(
                    CultureInfo.CurrentUICulture,
                    UIStrings.MergedResultConflictDisplayFormat,
                    CurrentConflictIndex,
                    TotalConflictCount);
        }
        else if (AutoResolvedCount > 0)
        {
            CurrentConflictDisplay = string.Format(
                CultureInfo.CurrentUICulture,
                UIStrings.MergedResultAllResolvedWithAutoResolvedFormat,
                AutoResolvedCount);
        }
        else
        {
            CurrentConflictDisplay = string.Format(
                CultureInfo.CurrentUICulture,
                UIStrings.MergedResultConflictDisplayFormat,
                0,
                0);
        }
    }

    #region Auto-resolve logic

    /// <summary>
    /// Attempts to automatically resolve trivially resolvable conflicts using
    /// deterministic three-way merge logic:
    /// - One side unchanged from base → take the other side
    /// - Both sides made the same change → take either
    /// </summary>
    private (string NewContent, IReadOnlyList<AutoResolvedRegion> AutoResolved) AttemptAutoResolve(string content)
    {
        var regions = _conflictParser.Parse(content);
        if (regions.Count == 0)
        {
            return (content, Array.Empty<AutoResolvedRegion>());
        }

        var lines = content.Split('\n').ToList();
        var autoResolved = new List<AutoResolvedRegion>();
        var lineOffset = 0;

        foreach (var region in regions)
        {
            var resolved = TryResolveConflict(region);
            if (resolved is null)
            {
                continue;
            }

            var startIdx = (region.StartLine - 1) + lineOffset;
            var endIdx = (region.EndLine - 1) + lineOffset;
            var originalCount = endIdx - startIdx + 1;

            lines.RemoveRange(startIdx, originalCount);

            var resolvedLines = resolved.Split('\n');
            for (var j = 0; j < resolvedLines.Length; j++)
            {
                lines.Insert(startIdx + j, resolvedLines[j]);
            }

            var newCount = resolvedLines.Length;
            var resolvedStartLine = startIdx + 1;
            var resolvedEndLine = startIdx + newCount;

            if (newCount > 0)
            {
                autoResolved.Add(new AutoResolvedRegion(resolvedStartLine, resolvedEndLine));
            }

            lineOffset += newCount - originalCount;
        }

        return (string.Join('\n', lines), autoResolved);
    }

    /// <summary>
    /// Returns the resolved content if the conflict can be trivially resolved,
    /// or null if it requires manual/AI intervention.
    /// </summary>
    private static string? TryResolveConflict(ConflictRegion region)
    {
        var local = NormalizeForComparison(region.LocalContent);
        var remote = NormalizeForComparison(region.RemoteContent);
        var baseContent = NormalizeForComparison(region.BaseContent);

        // Both sides made the same change → take either
        if (local is not null && remote is not null && local == remote)
        {
            return region.LocalContent;
        }

        // Only attempt base-relative resolution if base content is available
        if (baseContent is null)
        {
            return null;
        }

        // Local side unchanged from base → take remote
        if (local is not null && local == baseContent)
        {
            return region.RemoteContent;
        }

        // Remote side unchanged from base → take local
        if (remote is not null && remote == baseContent)
        {
            return region.LocalContent;
        }

        return null;
    }

    private static string? NormalizeForComparison(string? content)
    {
        if (content is null)
        {
            return null;
        }

        return content.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
    }

    #endregion
}
