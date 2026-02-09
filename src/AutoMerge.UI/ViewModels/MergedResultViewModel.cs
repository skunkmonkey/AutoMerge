using System.ComponentModel;
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
    private readonly IDiffCalculator _diffCalculator;
    private string _initialContent = string.Empty;
    private string _previousContent = string.Empty;
    private string _baseContent = string.Empty;
    private string _localContent = string.Empty;
    private string _remoteContent = string.Empty;
    private IReadOnlyList<ConflictRegion> _conflictRegions = Array.Empty<ConflictRegion>();
    private IReadOnlyList<ConflictRegion> _originalConflictRegions = Array.Empty<ConflictRegion>();
    private int _previousConflictCount = -1;
    private int _previousLineCount;

    public MergedResultViewModel(IConflictParser conflictParser, IDiffCalculator diffCalculator)
    {
        _conflictParser = conflictParser;
        _diffCalculator = diffCalculator;
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
    /// Per-line diff changes comparing base content to merged result content.
    /// Only lines that actually changed are included. Lines within unresolved
    /// conflict marker regions are excluded (the ConflictMarkerBackgroundRenderer
    /// handles those).
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<LineChange> _lineChanges = Array.Empty<LineChange>();

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

    // ── Conflict approval tracking ─────────────────────────────────────

    /// <summary>
    /// BeyondCompare-style approval items – one per original conflict.
    /// Each item shows a red ! (needs review) or green ✓ (approved) in the gutter.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<ConflictApprovalItem> _approvalItems = Array.Empty<ConflictApprovalItem>();

    /// <summary>
    /// True when every approval item is in the <see cref="ConflictApprovalState.Approved"/> state.
    /// The Accept button is gated on this.
    /// </summary>
    [ObservableProperty]
    private bool _allConflictsApproved;

    /// <summary>
    /// Number of approval items the user has already reviewed.
    /// </summary>
    [ObservableProperty]
    private int _approvedCount;

    /// <summary>
    /// Number of approval items that still need user review.
    /// </summary>
    [ObservableProperty]
    private int _unapprovedCount;

    /// <summary>
    /// The parsed conflict regions from the current content.
    /// Used by MainWindowViewModel to sync source pane scrolling.
    /// </summary>
    public IReadOnlyList<ConflictRegion> ConflictRegions => _conflictRegions;

    /// <summary>
    /// The original conflict regions parsed from the file before any resolution.
    /// Used by MainWindowViewModel to find corresponding content in source panes
    /// even after conflict markers have been removed by AI or manual edits.
    /// </summary>
    public IReadOnlyList<ConflictRegion> OriginalConflictRegions => _originalConflictRegions;

    private int NavigableConflictCount => ApprovalItems.Count;

    public bool CanGoNextConflict => NavigableConflictCount > 0 && CurrentConflictIndex < NavigableConflictCount;
    public bool CanGoPreviousConflict => NavigableConflictCount > 0 && CurrentConflictIndex > 1;

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

        // Parse original conflicts BEFORE auto-resolve (needed for approval items)
        var originalConflicts = _conflictParser.Parse(mergedContent);
        _originalConflictRegions = originalConflicts;

        // Attempt auto-resolution of trivially resolvable conflicts
        var (resolvedContent, autoResolved, wasResolved) = AttemptAutoResolve(mergedContent, originalConflicts);
        AutoResolvedRegions = autoResolved;
        AutoResolvedCount = autoResolved.Count;
        HasAutoResolved = autoResolved.Count > 0;

        // Parse remaining conflicts from the resolved content (before Content is set)
        var remainingConflicts = _conflictParser.Parse(resolvedContent);

        // Create approval items for all original conflicts
        CreateApprovalItems(originalConflicts, autoResolved, wasResolved, remainingConflicts);
        _previousConflictCount = remainingConflicts.Count;
        _previousContent = resolvedContent;
        _previousLineCount = CountLines(resolvedContent);

        Content = resolvedContent;
        UpdateValidationState();
    }

    partial void OnContentChanged(string value)
    {
        UpdateValidationState();

        var conflictsChanged = TotalConflictCount != _previousConflictCount;
        if (conflictsChanged)
        {
            _previousConflictCount = TotalConflictCount;
        }

        var currentLineCount = CountLines(value);
        UpdateApprovalItemStates(conflictsChanged, _previousLineCount, currentLineCount);

        // Update _previousContent AFTER approval states are computed so
        // line-delta calculations can compare old vs new content.
        _previousContent = value;
        _previousLineCount = currentLineCount;

        // Recompute diff-based line highlights (base → merged)
        RecomputeLineChanges();
    }

    private void UpdateValidationState()
    {
        HasConflictMarkers = _conflictParser.HasConflictMarkers(Content);
        IsDirty = !string.Equals(Content, _initialContent, StringComparison.Ordinal);

        // Update conflict regions (used for source pane sync when markers still exist)
        _conflictRegions = _conflictParser.Parse(Content);
        TotalConflictCount = _conflictRegions.Count;

        // Navigation is based on all conflict items, not raw conflict marker count.
        UpdateNavigationIndex();
        UpdateConflictDisplay();
        NextConflictCommand.NotifyCanExecuteChanged();
        PreviousConflictCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Adjusts <see cref="CurrentConflictIndex"/> to stay within the range of
    /// navigable conflict items (all conflicts, including approved ones).
    /// </summary>
    private void UpdateNavigationIndex()
    {
        if (NavigableConflictCount == 0)
        {
            CurrentConflictIndex = 0;
        }
        else if (CurrentConflictIndex == 0 && NavigableConflictCount > 0)
        {
            CurrentConflictIndex = 1;
        }
        else if (CurrentConflictIndex > NavigableConflictCount)
        {
            CurrentConflictIndex = NavigableConflictCount;
        }
    }

    private void GoToNextConflict()
    {
        if (CurrentConflictIndex < NavigableConflictCount)
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

    /// <summary>
    /// Scrolls to the current conflict item's position in the merged content.
    /// </summary>
    private void ScrollToCurrentConflict()
    {
        if (CurrentConflictIndex >= 1 && CurrentConflictIndex <= ApprovalItems.Count)
        {
            var item = ApprovalItems[CurrentConflictIndex - 1];
            // Set to 0 first to ensure PropertyChanged fires even if same line
            ScrollToLine = 0;
            ScrollToLine = item.StartLine;
        }
        NextConflictCommand.NotifyCanExecuteChanged();
        PreviousConflictCommand.NotifyCanExecuteChanged();
    }

    private void UpdateConflictDisplay()
    {
        if (NavigableConflictCount > 0)
        {
            CurrentConflictDisplay = AutoResolvedCount > 0
                ? string.Format(
                    CultureInfo.CurrentUICulture,
                    UIStrings.MergedResultConflictDisplayWithAutoResolvedFormat,
                    CurrentConflictIndex,
                    NavigableConflictCount,
                    AutoResolvedCount)
                : string.Format(
                    CultureInfo.CurrentUICulture,
                    UIStrings.MergedResultConflictDisplayFormat,
                    CurrentConflictIndex,
                    NavigableConflictCount);
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

    #region Diff-based line highlighting

    /// <summary>
    /// Computes per-line diff changes between the base content and the current
    /// merged result. Lines within unresolved conflict marker regions are excluded
    /// so they don't get double-highlighted (the ConflictMarkerBackgroundRenderer
    /// handles those regions).
    /// </summary>
    private void RecomputeLineChanges()
    {
        if (string.IsNullOrEmpty(_baseContent) || string.IsNullOrEmpty(Content))
        {
            LineChanges = Array.Empty<LineChange>();
            return;
        }

        var allChanges = _diffCalculator.CalculateDiffForNewText(_baseContent, Content);

        // Filter out lines that fall inside unresolved conflict marker regions.
        // Those regions are already visually handled by ConflictMarkerBackgroundRenderer.
        if (_conflictRegions.Count > 0)
        {
            var filtered = new List<LineChange>();
            foreach (var change in allChanges)
            {
                var isInConflict = false;
                foreach (var region in _conflictRegions)
                {
                    if (change.LineNumber >= region.StartLine && change.LineNumber <= region.EndLine)
                    {
                        isInConflict = true;
                        break;
                    }
                }

                if (!isInConflict)
                    filtered.Add(change);
            }

            LineChanges = filtered;
        }
        else
        {
            LineChanges = allChanges;
        }
    }

    #endregion

    #region Auto-resolve logic

    /// <summary>
    /// Attempts to automatically resolve trivially resolvable conflicts using
    /// deterministic three-way merge logic:
    /// - One side unchanged from base → take the other side
    /// - Both sides made the same change → take either
    /// </summary>
    private (string NewContent, IReadOnlyList<AutoResolvedRegion> AutoResolved, IReadOnlyList<bool> WasResolved) AttemptAutoResolve(
        string content, IReadOnlyList<ConflictRegion> originalConflicts)
    {
        if (originalConflicts.Count == 0)
        {
            return (content, Array.Empty<AutoResolvedRegion>(), Array.Empty<bool>());
        }

        var regions = originalConflicts;

        var lines = content.Split('\n').ToList();
        var autoResolved = new List<AutoResolvedRegion>();
        var wasResolved = new List<bool>();
        var lineOffset = 0;

        foreach (var region in regions)
        {
            var resolved = TryResolveConflict(region);
            if (resolved is null)
            {
                wasResolved.Add(false);
                continue;
            }

            wasResolved.Add(true);

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

        return (string.Join('\n', lines), autoResolved, wasResolved);
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

    #region Conflict approval logic

    /// <summary>
    /// Creates one approval item per original conflict. Auto-resolved conflicts
    /// start in <see cref="ConflictApprovalState.Resolved"/>; remaining ones in
    /// <see cref="ConflictApprovalState.Unresolved"/>.
    /// </summary>
    private void CreateApprovalItems(
        IReadOnlyList<ConflictRegion> originalConflicts,
        IReadOnlyList<AutoResolvedRegion> autoResolved,
        IReadOnlyList<bool> wasResolved,
        IReadOnlyList<ConflictRegion> remainingConflicts)
    {
        // Unsubscribe from previous items
        foreach (var old in ApprovalItems)
            old.PropertyChanged -= OnApprovalItemPropertyChanged;

        var items = new List<ConflictApprovalItem>();
        int autoIdx = 0;
        int unresolvedIdx = 0;

        for (int i = 0; i < originalConflicts.Count; i++)
        {
            ConflictApprovalState state;
            int startLine;
            int endLine;

            if (i < wasResolved.Count && wasResolved[i])
            {
                state = ConflictApprovalState.Resolved;
                if (autoIdx < autoResolved.Count)
                {
                    startLine = autoResolved[autoIdx].StartLine;
                    endLine = autoResolved[autoIdx].EndLine;
                }
                else
                {
                    startLine = originalConflicts[i].StartLine;
                    endLine = originalConflicts[i].EndLine;
                }
                autoIdx++;
            }
            else
            {
                state = ConflictApprovalState.Unresolved;
                if (unresolvedIdx < remainingConflicts.Count)
                {
                    startLine = remainingConflicts[unresolvedIdx].StartLine;
                    endLine = remainingConflicts[unresolvedIdx].EndLine;
                }
                else
                {
                    startLine = originalConflicts[i].StartLine;
                    endLine = originalConflicts[i].EndLine;
                }
                unresolvedIdx++;
            }

            var item = new ConflictApprovalItem
            {
                Index = i,
                StartLine = startLine,
                EndLine = endLine,
                State = state,
                ConflictKey = GetConflictKey(originalConflicts[i])
            };
            item.PropertyChanged += OnApprovalItemPropertyChanged;
            items.Add(item);
        }

        ApprovalItems = items;
        UpdateApprovalCounts();
        RebuildHighlightedRegions();
    }

    /// <summary>
    /// Updates approval item states and positions after edits.
    /// Ensures unresolved items follow current conflict markers, and resolved
    /// items stay anchored to their replacement regions as the document shifts.
    /// </summary>
    private void UpdateApprovalItemStates(bool conflictsChanged, int previousLineCount, int currentLineCount)
    {
        if (ApprovalItems.Count == 0)
        {
            return;
        }

        var unresolvedMap = MapCurrentConflictsToItems(_conflictRegions);
        var transitionedToResolved = 0;

        foreach (var item in ApprovalItems)
        {
            if (unresolvedMap.TryGetValue(item, out var region))
            {
                if (item.State != ConflictApprovalState.Unresolved)
                {
                    item.State = ConflictApprovalState.Unresolved;
                }

                item.StartLine = region.StartLine;
                item.EndLine = region.EndLine;
            }
            else if (item.State == ConflictApprovalState.Unresolved)
            {
                item.State = ConflictApprovalState.Resolved;
                transitionedToResolved++;
            }
        }

        var lineCountChanged = previousLineCount != currentLineCount;
        if (conflictsChanged || transitionedToResolved > 0)
        {
            var resolvedRegions = FindResolvedRegions(_originalConflictRegions, _initialContent, Content);
            ApplyResolvedRegions(resolvedRegions);
        }
        else if (lineCountChanged)
        {
            var delta = currentLineCount - previousLineCount;
            if (delta != 0 && !string.IsNullOrEmpty(_previousContent))
            {
                var diffLine = FindFirstDifferenceLine(_previousContent, Content);
                ApplyLineDeltaToResolvedItems(ApprovalItems, diffLine, delta);
            }
        }

        UpdateApprovalCounts();
        RebuildHighlightedRegions();
    }

    /// <summary>
    /// Rebuilds <see cref="AutoResolvedRegions"/> from all approval items whose
    /// state is <see cref="ConflictApprovalState.Resolved"/>. Approved items are
    /// excluded (they should look like regular text). Unresolved items are excluded
    /// (conflict markers handle their visual treatment).
    /// </summary>
    private void RebuildHighlightedRegions()
    {
        var regions = new List<AutoResolvedRegion>();
        foreach (var item in ApprovalItems)
        {
            if (item.State == ConflictApprovalState.Resolved && item.EndLine >= item.StartLine)
            {
                regions.Add(new AutoResolvedRegion(item.StartLine, item.EndLine));
            }
        }

        AutoResolvedRegions = regions;
    }

    private Dictionary<ConflictApprovalItem, ConflictRegion> MapCurrentConflictsToItems(IReadOnlyList<ConflictRegion> conflicts)
    {
        var map = new Dictionary<ConflictApprovalItem, ConflictRegion>();
        if (conflicts.Count == 0 || ApprovalItems.Count == 0)
        {
            return map;
        }

        var keyMap = new Dictionary<string, List<ConflictApprovalItem>>(StringComparer.Ordinal);
        foreach (var item in ApprovalItems)
        {
            var key = item.ConflictKey ?? string.Empty;
            if (!keyMap.TryGetValue(key, out var list))
            {
                list = new List<ConflictApprovalItem>();
                keyMap[key] = list;
            }
            list.Add(item);
        }

        var remainingRegions = new List<ConflictRegion>();

        foreach (var region in conflicts)
        {
            var key = GetConflictKey(region);
            if (keyMap.TryGetValue(key, out var list) && list.Count > 0)
            {
                var bestIndex = 0;
                var bestDistance = int.MaxValue;
                for (int i = 0; i < list.Count; i++)
                {
                    var distance = Math.Abs(list[i].StartLine - region.StartLine);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                var item = list[bestIndex];
                list.RemoveAt(bestIndex);
                if (!map.ContainsKey(item))
                {
                    map[item] = region;
                }
                else
                {
                    remainingRegions.Add(region);
                }
            }
            else
            {
                remainingRegions.Add(region);
            }
        }

        if (remainingRegions.Count > 0)
        {
            var unmatchedItems = new List<ConflictApprovalItem>();
            foreach (var item in ApprovalItems)
            {
                if (!map.ContainsKey(item))
                {
                    unmatchedItems.Add(item);
                }
            }

            foreach (var region in remainingRegions)
            {
                if (unmatchedItems.Count == 0)
                {
                    break;
                }

                var bestIndex = 0;
                var bestDistance = int.MaxValue;
                for (int i = 0; i < unmatchedItems.Count; i++)
                {
                    var distance = Math.Abs(unmatchedItems[i].StartLine - region.StartLine);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                map[unmatchedItems[bestIndex]] = region;
                unmatchedItems.RemoveAt(bestIndex);
            }
        }

        return map;
    }

    private void ApplyResolvedRegions(IReadOnlyList<(int Start, int End)> resolvedRegions)
    {
        if (resolvedRegions.Count == 0)
        {
            return;
        }

        var resolvedIndex = 0;
        foreach (var item in ApprovalItems)
        {
            if (item.State == ConflictApprovalState.Unresolved)
            {
                continue;
            }

            if (resolvedIndex >= resolvedRegions.Count)
            {
                break;
            }

            var region = resolvedRegions[resolvedIndex++];
            if (region.End < region.Start)
            {
                continue;
            }

            item.StartLine = region.Start;
            item.EndLine = region.End;
        }
    }

    private static void ApplyLineDeltaToResolvedItems(IReadOnlyList<ConflictApprovalItem> items, int diffLine, int delta)
    {
        if (delta == 0 || diffLine <= 0)
        {
            return;
        }

        foreach (var item in items)
        {
            if (item.State == ConflictApprovalState.Unresolved)
            {
                continue;
            }

            var start = item.StartLine;
            var end = item.EndLine;

            if (start >= diffLine)
            {
                var shiftedStart = Math.Max(1, start + delta);
                var shiftedEnd = Math.Max(shiftedStart, end + delta);
                item.StartLine = shiftedStart;
                item.EndLine = shiftedEnd;
            }
            else if (end >= diffLine)
            {
                item.EndLine = Math.Max(start, end + delta);
            }
        }
    }

    private static int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        var count = 1;
        foreach (var ch in content)
        {
            if (ch == '\n')
            {
                count++;
            }
        }

        return count;
    }

    private static int FindFirstDifferenceLine(string oldContent, string newContent)
    {
        if (string.IsNullOrEmpty(oldContent) || string.IsNullOrEmpty(newContent))
        {
            return 1;
        }

        var oldLines = SplitLines(oldContent);
        var newLines = SplitLines(newContent);
        var min = Math.Min(oldLines.Length, newLines.Length);

        for (var i = 0; i < min; i++)
        {
            if (!string.Equals(oldLines[i], newLines[i], StringComparison.Ordinal))
            {
                return i + 1;
            }
        }

        return min + 1;
    }

    private static string GetConflictKey(ConflictRegion region)
    {
        var baseText = NormalizeForKey(region.BaseContent);
        var localText = NormalizeForKey(region.LocalContent);
        var remoteText = NormalizeForKey(region.RemoteContent);

        var baseHash = ComputeHash64(baseText).ToString("x16", CultureInfo.InvariantCulture);
        var localHash = ComputeHash64(localText).ToString("x16", CultureInfo.InvariantCulture);
        var remoteHash = ComputeHash64(remoteText).ToString("x16", CultureInfo.InvariantCulture);

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}:{2}:{3}:{4}:{5}",
            baseHash, baseText.Length,
            localHash, localText.Length,
            remoteHash, remoteText.Length);
    }

    private static string NormalizeForKey(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        return content.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd('\n');
    }

    private static ulong ComputeHash64(string text)
    {
        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;
        ulong hash = offset;

        foreach (var ch in text)
        {
            hash ^= ch;
            hash *= prime;
        }

        return hash;
    }

    private static string[] SplitLines(string content)
    {
        return content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
    }

    private static (int Start, int End)? ClampRegion(int start, int end, int maxLine)
    {
        if (maxLine <= 0)
        {
            return null;
        }

        var safeStart = Math.Clamp(start, 1, maxLine);
        var safeEnd = Math.Clamp(end, safeStart, maxLine);
        return (safeStart, safeEnd);
    }

    /// <summary>
    /// Maps original conflict regions from marker-laden content to the current
    /// merged content using LCS-based line alignment. This avoids false matches
    /// on common tokens (such as braces) that can truncate region boundaries.
    /// Returns a list of 1-based (Start, End) line ranges in the new content,
    /// preserving original conflict order.
    /// </summary>
    private static List<(int Start, int End)> FindResolvedRegions(
        IReadOnlyList<ConflictRegion> originalConflicts,
        string oldContent,
        string newContent)
    {
        if (originalConflicts.Count == 0 || string.IsNullOrEmpty(oldContent) || string.IsNullOrEmpty(newContent))
        {
            return new List<(int, int)>();
        }

        var oldLines = SplitLines(oldContent);
        var newLines = SplitLines(newContent);
        if (newLines.Length == 0)
        {
            return new List<(int, int)>();
        }

        var oldToNew = BuildOldToNewLineMap(oldLines, newLines);
        var regions = new List<(int Start, int End)>(originalConflicts.Count);
        var previousEnd = 0;

        foreach (var conflict in originalConflicts)
        {
            var start = 1;
            var end = newLines.Length;

            var previousStableOldLine = FindNearestMappedOldLineBefore(oldToNew, conflict.StartLine - 1);
            if (previousStableOldLine > 0)
            {
                start = oldToNew[previousStableOldLine] + 1;
            }

            var nextStableOldLine = FindNearestMappedOldLineAfter(oldToNew, conflict.EndLine + 1);
            if (nextStableOldLine > 0)
            {
                end = oldToNew[nextStableOldLine] - 1;
            }

            start = Math.Max(start, previousEnd + 1);

            var clamped = ClampRegion(start, end, newLines.Length);
            if (clamped.HasValue)
            {
                regions.Add(clamped.Value);
                previousEnd = clamped.Value.End;
            }
        }

        return regions;
    }

    private static int[] BuildOldToNewLineMap(string[] oldLines, string[] newLines)
    {
        var oldCount = oldLines.Length;
        var newCount = newLines.Length;
        var lcs = new int[oldCount + 1, newCount + 1];

        for (var oldIndex = oldCount - 1; oldIndex >= 0; oldIndex--)
        {
            for (var newIndex = newCount - 1; newIndex >= 0; newIndex--)
            {
                if (string.Equals(oldLines[oldIndex], newLines[newIndex], StringComparison.Ordinal))
                {
                    lcs[oldIndex, newIndex] = lcs[oldIndex + 1, newIndex + 1] + 1;
                }
                else
                {
                    lcs[oldIndex, newIndex] = Math.Max(lcs[oldIndex + 1, newIndex], lcs[oldIndex, newIndex + 1]);
                }
            }
        }

        var oldToNew = new int[oldCount + 1];
        var oi = 0;
        var ni = 0;

        while (oi < oldCount && ni < newCount)
        {
            if (string.Equals(oldLines[oi], newLines[ni], StringComparison.Ordinal))
            {
                oldToNew[oi + 1] = ni + 1;
                oi++;
                ni++;
                continue;
            }

            if (lcs[oi + 1, ni] >= lcs[oi, ni + 1])
            {
                oi++;
            }
            else
            {
                ni++;
            }
        }

        return oldToNew;
    }

    private static int FindNearestMappedOldLineBefore(int[] oldToNew, int line)
    {
        for (var candidate = Math.Min(line, oldToNew.Length - 1); candidate >= 1; candidate--)
        {
            if (oldToNew[candidate] > 0)
            {
                return candidate;
            }
        }

        return 0;
    }

    private static int FindNearestMappedOldLineAfter(int[] oldToNew, int line)
    {
        for (var candidate = Math.Max(1, line); candidate < oldToNew.Length; candidate++)
        {
            if (oldToNew[candidate] > 0)
            {
                return candidate;
            }
        }

        return 0;
    }

    private void UpdateApprovalCounts()
    {
        if (ApprovalItems.Count == 0)
        {
            AllConflictsApproved = true;
            ApprovedCount = 0;
            UnapprovedCount = 0;
            return;
        }

        int approved = 0;
        int unapproved = 0;

        foreach (var item in ApprovalItems)
        {
            if (item.State == ConflictApprovalState.Approved)
                approved++;
            else
                unapproved++;
        }

        ApprovedCount = approved;
        UnapprovedCount = unapproved;
        AllConflictsApproved = unapproved == 0;
    }

    private void OnApprovalItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConflictApprovalItem.State))
        {
            UpdateApprovalCounts();
            UpdateNavigationIndex();
            UpdateConflictDisplay();
            NextConflictCommand.NotifyCanExecuteChanged();
            PreviousConflictCommand.NotifyCanExecuteChanged();

            // Rebuild highlighted regions so approved items lose their green tint
            // and un-approved items regain it.
            RebuildHighlightedRegions();
        }
    }

    #endregion
}
