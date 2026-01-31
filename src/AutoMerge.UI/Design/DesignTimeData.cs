using AutoMerge.Core.Models;

namespace AutoMerge.UI.Design;

public static class DesignTimeData
{
    public static readonly ChatMessage[] SampleMessages =
    [
        new ChatMessage(ChatRole.System, "Design-time session loaded.", DateTimeOffset.UtcNow),
        new ChatMessage(ChatRole.User, "Explain this conflict.", DateTimeOffset.UtcNow),
        new ChatMessage(ChatRole.Assistant, "The conflict is due to divergent edits in the same method.", DateTimeOffset.UtcNow)
    ];
}
