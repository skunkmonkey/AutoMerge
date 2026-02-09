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

public sealed record UserPreferences(
    DefaultBias DefaultBias,
    bool AutoAnalyzeOnLoad,
    Theme Theme,
    string AiModel = "GPT-5 mini")
{
    public static UserPreferences Default { get; } = new(DefaultBias.Balanced, true, Theme.System, "GPT-5 mini");
}
