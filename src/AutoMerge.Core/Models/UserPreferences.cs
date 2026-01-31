namespace AutoMerge.Core.Models;

public enum DefaultBias
{
    Balanced,
    PreferLocal,
    PreferRemote
}

public enum Theme
{
    System,
    Light,
    Dark
}

public sealed record UserPreferences(DefaultBias DefaultBias, bool AutoAnalyzeOnLoad, Theme Theme)
{
    public static UserPreferences Default { get; } = new(DefaultBias.Balanced, true, Theme.System);
}
