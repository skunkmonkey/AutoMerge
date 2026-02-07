namespace AutoMerge.Core.Models;

/// <summary>
/// Tracks a region in the merged content that was automatically resolved
/// by the three-way merge logic (not AI). Used for visual highlighting.
/// </summary>
public sealed record AutoResolvedRegion(int StartLine, int EndLine);
