using System;

namespace AutoMerge.Core.Models;

public enum ChatRole
{
    User,
    Assistant,
    System
}

public sealed record ChatMessage(ChatRole Role, string Content, DateTimeOffset Timestamp);
