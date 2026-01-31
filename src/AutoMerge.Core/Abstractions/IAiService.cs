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
        CancellationToken cancellationToken = default);

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
}
