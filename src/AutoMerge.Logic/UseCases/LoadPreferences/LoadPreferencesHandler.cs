using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.LoadPreferences;

public sealed class LoadPreferencesHandler
{
    private readonly IConfigurationService _configurationService;

    public LoadPreferencesHandler(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public Task<UserPreferences> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _configurationService.LoadPreferencesAsync(cancellationToken);
    }
}
