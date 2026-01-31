using AutoMerge.Core.Models;

namespace AutoMerge.Application.UseCases.ProposeResolution;

public sealed record ProposeResolutionCommand(UserPreferences? Preferences = null);
