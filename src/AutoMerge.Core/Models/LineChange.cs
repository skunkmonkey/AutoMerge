namespace AutoMerge.Core.Models;

public sealed record LineChange(int LineNumber, LineChangeType ChangeType, string Content);
