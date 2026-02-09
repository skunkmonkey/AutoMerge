using System.Threading;
using System.Threading.Tasks;
using AutoMerge.Core.Models;

namespace AutoMerge.Core.Abstractions;

public interface IConfigurationService
{
    Task<UserPreferences> LoadPreferencesAsync(CancellationToken cancellationToken);

    Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken);

    Task ResetPreferencesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> LoadAiModelOptionsAsync(CancellationToken cancellationToken);
}
