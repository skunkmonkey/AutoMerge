using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.ProposeResolution;

public sealed record ProposeResolutionCommand(UserPreferences? Preferences = null);
