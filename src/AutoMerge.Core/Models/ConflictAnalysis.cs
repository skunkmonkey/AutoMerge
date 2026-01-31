namespace AutoMerge.Core.Models;

public sealed record ConflictAnalysis(
    string LocalChangeDescription,
    string RemoteChangeDescription,
    string ConflictReason,
    string SuggestedApproach);
