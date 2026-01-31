namespace AutoMerge.Core.Models;

public enum SessionState
{
    Created,
    Loading,
    Ready,
    Analyzing,
    ResolutionProposed,
    Refining,
    UserEditing,
    Validated,
    Saved,
    Cancelled
}
