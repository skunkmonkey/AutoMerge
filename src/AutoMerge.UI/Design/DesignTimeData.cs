using AutoMerge.Core.Models;
using AutoMerge.UI.Localization;

namespace AutoMerge.UI.Design;

public static class DesignTimeData
{
    public static readonly ChatMessage[] SampleMessages =
    [
        new ChatMessage(ChatRole.System, UIStrings.DesignTimeSessionLoaded, DateTimeOffset.UtcNow),
        new ChatMessage(ChatRole.User, UIStrings.DesignTimeExplainConflict, DateTimeOffset.UtcNow),
        new ChatMessage(ChatRole.Assistant, UIStrings.DesignTimeConflictExplanation, DateTimeOffset.UtcNow)
    ];
}
