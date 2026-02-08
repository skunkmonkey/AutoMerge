using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.ProposeResolution;

public sealed record ProposeResolutionCommand(
    UserPreferences? Preferences = null,
    Action<string>? OnLocalIntentChunk = null,
    Action<string>? OnRemoteIntentChunk = null,
    Action<string, string>? OnBusyMessageChanged = null);
