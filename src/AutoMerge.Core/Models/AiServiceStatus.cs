namespace AutoMerge.Core.Models;

public sealed record AiServiceStatus(
    bool IsAvailable,
    bool IsAuthenticated,
    string? ErrorMessage,
    string? ActiveModel = null);
