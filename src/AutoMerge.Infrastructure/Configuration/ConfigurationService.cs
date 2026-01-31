using System.Text.Json;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Infrastructure.Configuration;

public sealed class ConfigurationService : IConfigurationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true
    };

    public async Task<UserPreferences> LoadPreferencesAsync(CancellationToken cancellationToken)
    {
        var path = GetPreferencesPath();
        if (!File.Exists(path))
        {
            return UserPreferences.Default;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<UserPreferences>(json, SerializerOptions) ?? UserPreferences.Default;
    }

    public async Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken)
    {
        var path = GetPreferencesPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(preferences, SerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    public Task ResetPreferencesAsync(CancellationToken cancellationToken)
    {
        var path = GetPreferencesPath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private static string GetPreferencesPath()
    {
        return Path.Combine(PlatformPaths.GetConfigDirectory(), "preferences.json");
    }
}
