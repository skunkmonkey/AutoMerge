using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMerge.Core.Models;

namespace AutoMerge.Core.Abstractions;

public interface IAiService
{
    Task<AiServiceStatus> GetStatusAsync(CancellationToken cancellationToken);

    Task<ConflictAnalysis> AnalyzeConflictAsync(
        MergeSession session,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    Task<MergeResolution> ProposeResolutionAsync(
        MergeSession session,
        UserPreferences? preferences = null,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default,
        string? localIntent = null,
        string? remoteIntent = null);

    Task<MergeResolution> RefineResolutionAsync(
        MergeSession session,
        string userMessage,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default);

    Task<string> ExplainChangesAsync(
        MergeSession session,
        int startLine,
        int endLine,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Researches the intent behind changes in a specific file version (Local or Remote)
    /// by comparing it against the base version. Each call uses a fresh context window.
    /// </summary>
    Task<string> ResearchIntentAsync(
        MergeSession session,
        FileVersion version,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default);
}
