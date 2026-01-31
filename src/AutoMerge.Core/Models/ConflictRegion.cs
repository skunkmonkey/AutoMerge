namespace AutoMerge.Core.Models;

public sealed record ConflictRegion(
    int StartLine,
    int EndLine,
    string? BaseContent,
    string? LocalContent,
    string? RemoteContent);
