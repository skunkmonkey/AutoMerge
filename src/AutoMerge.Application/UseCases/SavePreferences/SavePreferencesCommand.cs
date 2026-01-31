using AutoMerge.Core.Models;

namespace AutoMerge.Application.UseCases.SavePreferences;

public sealed record SavePreferencesCommand(UserPreferences Preferences);
