using System.Runtime.Versioning;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Win32;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Infrastructure.Configuration;

public sealed class ConfigurationService : IConfigurationService
{
    private const string RegistryBaseKey = @"Software\AutoMerge";
    private const string PreferencesValueName = "PreferencesJson";
    private const string ModelCatalogFileName = "ai-models.xml";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true
    };

    public async Task<UserPreferences> LoadPreferencesAsync(CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            var registryJson = LoadPreferencesJsonFromRegistry();
            return DeserializePreferences(registryJson);
        }

        var path = GetPreferencesPath();
        if (!File.Exists(path))
        {
            return UserPreferences.Default;
        }

        var fileJson = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return DeserializePreferences(fileJson);
    }

    public async Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(preferences, SerializerOptions);

        if (OperatingSystem.IsWindows())
        {
            SavePreferencesJsonToRegistry(json);
            return;
        }

        var path = GetPreferencesPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    public Task ResetPreferencesAsync(CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            DeletePreferencesFromRegistry();
            return Task.CompletedTask;
        }

        var path = GetPreferencesPath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> LoadAiModelOptionsAsync(CancellationToken cancellationToken)
    {
        var path = GetModelCatalogPath();
        if (!File.Exists(path))
        {
            return Task.FromResult<IReadOnlyList<string>>(new[] { UserPreferences.Default.AiModel });
        }

        try
        {
            using var stream = File.OpenRead(path);
            var doc = XDocument.Load(stream);
            var models = doc.Root?
                .Elements("model")
                .Select(element => element.Value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (models is null || models.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<string>>(new[] { UserPreferences.Default.AiModel });
            }

            return Task.FromResult<IReadOnlyList<string>>(models);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<string>>(new[] { UserPreferences.Default.AiModel });
        }
    }

    private static string GetPreferencesPath()
    {
        return Path.Combine(PlatformPaths.GetConfigDirectory(), "preferences.json");
    }

    private static string GetModelCatalogPath()
    {
        return Path.Combine(AppContext.BaseDirectory, ModelCatalogFileName);
    }

    private static UserPreferences DeserializePreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return UserPreferences.Default;
        }

        return JsonSerializer.Deserialize<UserPreferences>(json, SerializerOptions) ?? UserPreferences.Default;
    }

    [SupportedOSPlatform("windows")]
    private static string? LoadPreferencesJsonFromRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryBaseKey, false);
            return key?.GetValue(PreferencesValueName) as string;
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static void SavePreferencesJsonToRegistry(string json)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryBaseKey, true);
            key?.SetValue(PreferencesValueName, json, RegistryValueKind.String);
        }
        catch
        {
            // Swallow registry exceptions to keep the app usable.
        }
    }

    [SupportedOSPlatform("windows")]
    private static void DeletePreferencesFromRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryBaseKey, true);
            key?.DeleteValue(PreferencesValueName, false);
        }
        catch
        {
            // Swallow registry exceptions to keep the app usable.
        }
    }
}
