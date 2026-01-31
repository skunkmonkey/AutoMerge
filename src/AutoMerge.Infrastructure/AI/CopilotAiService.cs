using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.Infrastructure.AI.Prompts;

namespace AutoMerge.Infrastructure.AI;

public sealed class CopilotAiService : IAiService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    private readonly dynamic _client;

    public CopilotAiService(object copilotClient)
    {
        _client = copilotClient ?? throw new ArgumentNullException(nameof(copilotClient));
    }

    public async Task<AiServiceStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var status = await ExecuteWithTimeoutAsync(async token =>
            {
                try
                {
                    return await _client.GetStatusAsync(token).ConfigureAwait(false);
                }
                catch
                {
                    return null;
                }
            }, cancellationToken).ConfigureAwait(false);

            if (status is null)
            {
                return new AiServiceStatus(true, true, null);
            }

            var isAuthenticated = TryGetBool(status, "IsAuthenticated") ?? true;
            var isAvailable = TryGetBool(status, "IsAvailable") ?? true;
            var errorMessage = TryGetString(status, "ErrorMessage");
            return new AiServiceStatus(isAvailable, isAuthenticated, errorMessage);
        }
        catch (Exception ex)
        {
            throw new AiServiceException("Failed to get Copilot status.", ex);
        }
    }

    public async Task<ConflictAnalysis> AnalyzeConflictAsync(
        MergeSession session,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = BuildAnalysisPrompt(session);
            var response = await SendPromptAsync(SystemPrompts.MergeAgentSystemPrompt, prompt, chunk => progress?.Report(chunk), cancellationToken)
                .ConfigureAwait(false);

            return new ConflictAnalysis(response, response, response, response);
        }
        catch (Exception ex)
        {
            throw new AiServiceException("Copilot analysis failed.", ex);
        }
    }

    public async Task<MergeResolution> ProposeResolutionAsync(
        MergeSession session,
        UserPreferences? preferences = null,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = BuildResolutionPrompt(session, preferences ?? UserPreferences.Default);
            var response = await SendPromptAsync(SystemPrompts.MergeAgentSystemPrompt, prompt, onChunk, cancellationToken).ConfigureAwait(false);
            return new MergeResolution(response, response, 0.5);
        }
        catch (Exception ex)
        {
            throw new AiServiceException("Copilot resolution proposal failed.", ex);
        }
    }

    public async Task<MergeResolution> RefineResolutionAsync(
        MergeSession session,
        string userMessage,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentResolution = session.ProposedResolution?.ResolvedContent ?? string.Empty;
            var prompt = SystemPrompts.RefinementPromptTemplate
                .Replace("{USER_MESSAGE}", userMessage)
                .Replace("{CURRENT_RESOLUTION}", currentResolution);

            var response = await SendPromptAsync(SystemPrompts.MergeAgentSystemPrompt, prompt, onChunk, cancellationToken).ConfigureAwait(false);
            return new MergeResolution(response, response, 0.5);
        }
        catch (Exception ex)
        {
            throw new AiServiceException("Copilot refinement failed.", ex);
        }
    }

    public async Task<string> ExplainChangesAsync(
        MergeSession session,
        int startLine,
        int endLine,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = $"Explain the changes between lines {startLine}-{endLine} in the current merge context.";
            return await SendPromptAsync(SystemPrompts.MergeAgentSystemPrompt, prompt, null, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new AiServiceException("Copilot explanation failed.", ex);
        }
    }

    private async Task<string> SendPromptAsync(
        string systemPrompt,
        string userPrompt,
        Action<string>? onChunk,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithTimeoutAsync(async token =>
        {
            dynamic session = await _client.CreateSessionAsync(systemPrompt, token).ConfigureAwait(false);
            var response = await session.SendAsync(userPrompt, onChunk, token).ConfigureAwait(false);
            return response?.ToString() ?? string.Empty;
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<T> ExecuteWithTimeoutAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        return await operation(linkedCts.Token).ConfigureAwait(false);
    }

    private static bool? TryGetBool(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        if (property?.GetValue(target) is bool value)
        {
            return value;
        }

        return null;
    }

    private static string? TryGetString(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        return property?.GetValue(target)?.ToString();
    }

    private static string BuildAnalysisPrompt(MergeSession session)
    {
        var mergedContent = session.ConflictFile?.OriginalContent ?? string.Empty;
        return SystemPrompts.AnalysisPromptTemplate
            .Replace("{BASE}", SafeReadFile(session.MergeInput.BasePath))
            .Replace("{LOCAL}", SafeReadFile(session.MergeInput.LocalPath))
            .Replace("{REMOTE}", SafeReadFile(session.MergeInput.RemotePath))
            .Replace("{MERGED}", mergedContent);
    }

    private static string BuildResolutionPrompt(MergeSession session, UserPreferences preferences)
    {
        var mergedContent = session.ConflictFile?.OriginalContent ?? string.Empty;
        var preferenceText = $"DefaultBias: {preferences.DefaultBias}; AutoAnalyzeOnLoad: {preferences.AutoAnalyzeOnLoad}; Theme: {preferences.Theme}";

        return SystemPrompts.ResolutionPromptTemplate
            .Replace("{PREFERENCES}", preferenceText)
            .Replace("{BASE}", SafeReadFile(session.MergeInput.BasePath))
            .Replace("{LOCAL}", SafeReadFile(session.MergeInput.LocalPath))
            .Replace("{REMOTE}", SafeReadFile(session.MergeInput.RemotePath))
            .Replace("{MERGED}", mergedContent);
    }

    private static string SafeReadFile(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
