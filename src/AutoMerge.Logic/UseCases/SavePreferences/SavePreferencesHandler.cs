using AutoMerge.Core.Abstractions;

namespace AutoMerge.Logic.UseCases.SavePreferences;

public sealed class SavePreferencesHandler
{
    private readonly IConfigurationService _configurationService;

    public SavePreferencesHandler(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public Task ExecuteAsync(SavePreferencesCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        return _configurationService.SavePreferencesAsync(command.Preferences, cancellationToken);
    }
}
