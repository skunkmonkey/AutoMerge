using System.Globalization;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.Infrastructure.Localization;

namespace AutoMerge.Infrastructure.AI;

public sealed class MockAiService : IAiService
{
    private readonly ConflictAnalysis _analysis;
    private readonly MergeResolution _resolution;
    private readonly string _explanation;
    private readonly TimeSpan _delay;

    public MockAiService(
        ConflictAnalysis? analysis = null,
        MergeResolution? resolution = null,
        string? explanation = null,
        TimeSpan? delay = null)
    {
        _analysis = analysis ?? new ConflictAnalysis(
            InfrastructureStrings.MockLocalChangeSummary,
            InfrastructureStrings.MockRemoteChangeSummary,
            InfrastructureStrings.MockConflictReason,
            InfrastructureStrings.MockSuggestedApproach);

        _resolution = resolution ?? new MergeResolution(
            InfrastructureStrings.MockResolvedContent,
            InfrastructureStrings.MockResolutionExplanation,
            0.75);

        _explanation = explanation ?? InfrastructureStrings.MockExplanation;
        _delay = delay ?? TimeSpan.FromMilliseconds(100);
    }

    public Task<AiServiceStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new AiServiceStatus(true, true, null, InfrastructureStrings.MockModelName));
    }

    public async Task<ConflictAnalysis> AnalyzeConflictAsync(
        MergeSession session,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await StreamAsync(_analysis.SuggestedApproach, chunk => progress?.Report(chunk), cancellationToken).ConfigureAwait(false);
        return _analysis;
    }

    public async Task<MergeResolution> ProposeResolutionAsync(
        MergeSession session,
        UserPreferences? preferences = null,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default,
        string? localIntent = null,
        string? remoteIntent = null)
    {
        await StreamAsync(_resolution.ResolvedContent, onChunk, cancellationToken).ConfigureAwait(false);
        return _resolution;
    }

    public async Task<MergeResolution> RefineResolutionAsync(
        MergeSession session,
        string userMessage,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        var refined = _resolution with
        {
            Explanation = string.Format(
                CultureInfo.CurrentCulture,
                InfrastructureStrings.RefinedMessageFormat,
                userMessage)
        };
        await StreamAsync(refined.ResolvedContent, onChunk, cancellationToken).ConfigureAwait(false);
        return refined;
    }

    public Task<string> ExplainChangesAsync(
        MergeSession session,
        int startLine,
        int endLine,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_explanation);
    }

    public async Task<string> ResearchIntentAsync(
        MergeSession session,
        FileVersion version,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        var intent = version == FileVersion.Local
            ? InfrastructureStrings.MockLocalIntent
            : InfrastructureStrings.MockRemoteIntent;

        await StreamAsync(intent, onChunk, cancellationToken).ConfigureAwait(false);
        return intent;
    }

    private async Task StreamAsync(string text, Action<string>? onChunk, CancellationToken cancellationToken)
    {
        if (onChunk is null || string.IsNullOrEmpty(text))
        {
            return;
        }

        const int chunkSize = 20;
        for (var i = 0; i < text.Length; i += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunk = text.Substring(i, Math.Min(chunkSize, text.Length - i));
            onChunk(chunk);
            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
        }
    }
}
