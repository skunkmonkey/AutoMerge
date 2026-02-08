using AutoMerge.Core.Abstractions;

namespace AutoMerge.Logic.UseCases.LoadAiModelOptions;

public sealed class LoadAiModelOptionsHandler
{
    private readonly IConfigurationService _configurationService;

    public LoadAiModelOptionsHandler(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public Task<IReadOnlyList<string>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _configurationService.LoadAiModelOptionsAsync(cancellationToken);
    }
}
