using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoMerge.UI.ViewModels;

/// <summary>
/// Tracks the approval state of a single conflict region.
/// BeyondCompare-style: the user must review and approve every resolved
/// conflict before the Accept button becomes available.
/// </summary>
public enum ConflictApprovalState
{
    /// <summary>Conflict still has markers — cannot be approved until resolved.</summary>
    Unresolved,

    /// <summary>Conflict is resolved (auto / AI / manual) but not yet approved by user.</summary>
    Resolved,

    /// <summary>User has reviewed and approved this resolution.</summary>
    Approved
}

/// <summary>
/// Represents one conflict's approval state in the merged result gutter.
/// </summary>
public sealed partial class ConflictApprovalItem : ObservableObject
{
    /// <summary>Index of this conflict in the original conflict list (0-based).</summary>
    [ObservableProperty]
    private int _index;

    /// <summary>
    /// The 1-based line number in the current merged content where this conflict
    /// starts. Used by the gutter margin to position the indicator.
    /// </summary>
    [ObservableProperty]
    private int _startLine;

    /// <summary>
    /// The 1-based line number in the current merged content where this conflict
    /// region ends. Used by the highlight renderer to determine the range.
    /// </summary>
    [ObservableProperty]
    private int _endLine;

    /// <summary>Current approval state.</summary>
    [ObservableProperty]
    private ConflictApprovalState _state = ConflictApprovalState.Unresolved;

    /// <summary>
    /// Stable fingerprint of the original conflict content used to map
    /// unresolved conflicts to their approval items after edits.
    /// </summary>
    public string ConflictKey { get; init; } = string.Empty;
}
