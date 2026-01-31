namespace AutoMerge.Core.Models;

public sealed record MergeResolution(
    string ResolvedContent,
    string Explanation,
    double Confidence);
