using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.SavePreferences;

public sealed record SavePreferencesCommand(UserPreferences Preferences);
