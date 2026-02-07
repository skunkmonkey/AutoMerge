using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.Infrastructure.AI.Prompts;
using GitHub.Copilot.SDK;

namespace AutoMerge.Infrastructure.AI;

/// <summary>
/// AI service implementation using the GitHub Copilot SDK.
/// Requires GitHub Copilot CLI to be installed and authenticated.
/// </summary>
public sealed class CopilotAiService : IAiService, IAsyncDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    private readonly CopilotClientOptions _options;
    private CopilotClient? _client;
    private CopilotSession? _currentSession;
    private bool _isDisposed;
    private string _activeModel = UserPreferences.Default.AiModel;

    public CopilotAiService(CopilotClientOptions? options = null)
    {
        _options = options ?? new CopilotClientOptions
        {
            AutoStart = false,
            UseLoggedInUser = true
        };
    }

    /// <summary>
    /// Sets the active model used for subsequent AI requests.
    /// </summary>
    public void SetModel(string model)
    {
        if (!string.IsNullOrWhiteSpace(model))
            _activeModel = model;
    }

    public async Task<AiServiceStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Try to start a client and ping it to check status
            await EnsureClientStartedAsync(cancellationToken).ConfigureAwait(false);

            if (_client is null)
            {
                return new AiServiceStatus(false, false, "Failed to start Copilot client.");
            }

            var pingResult = await _client.PingAsync().ConfigureAwait(false);
            return new AiServiceStatus(true, true, null, _activeModel);
        }
        catch (FileNotFoundException)
        {
            return new AiServiceStatus(false, false, "GitHub Copilot CLI not found. Please install it from https://github.com/github/copilot-cli");
        }
        catch (Exception ex)
        {
            // Check for common authentication issues
            if (ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase))
            {
                return new AiServiceStatus(true, false, "Please authenticate with GitHub Copilot CLI: run 'copilot auth login'");
            }

            return new AiServiceStatus(false, false, $"Copilot connection failed: {ex.Message}");
        }
    }

    public async Task<ConflictAnalysis> AnalyzeConflictAsync(
        MergeSession session,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureClientStartedAsync(cancellationToken).ConfigureAwait(false);

            var prompt = BuildAnalysisPrompt(session);
            var response = await SendMessageAsync(
                SystemPrompts.MergeAgentSystemPrompt,
                prompt,
                chunk => progress?.Report(chunk),
                cancellationToken).ConfigureAwait(false);

            // Parse structured analysis from response (simplified)
            return new ConflictAnalysis(
                ExtractSection(response, "Local Changes:", "Remote Changes:") ?? response,
                ExtractSection(response, "Remote Changes:", "Conflict Reason:") ?? response,
                ExtractSection(response, "Conflict Reason:", "Suggested Approach:") ?? response,
                ExtractSection(response, "Suggested Approach:", null) ?? response);
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
            var prefs = preferences ?? UserPreferences.Default;
            SetModel(prefs.AiModel);
            await EnsureClientStartedAsync(cancellationToken).ConfigureAwait(false);

            var prompt = BuildResolutionPrompt(session, prefs);
            var response = await SendMessageAsync(
                SystemPrompts.MergeAgentSystemPrompt,
                prompt,
                onChunk,
                cancellationToken).ConfigureAwait(false);

            // Extract the resolution content (between code fences if present)
            var resolvedContent = ExtractCodeBlock(response) ?? response;
            var explanation = ExtractSection(response, "Explanation:", null) ?? "AI-proposed resolution";

            return new MergeResolution(resolvedContent, explanation, 0.75);
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
            await EnsureClientStartedAsync(cancellationToken).ConfigureAwait(false);

            var currentResolution = session.ProposedResolution?.ResolvedContent ?? string.Empty;
            var prompt = SystemPrompts.RefinementPromptTemplate
                .Replace("{USER_MESSAGE}", userMessage)
                .Replace("{CURRENT_RESOLUTION}", currentResolution);

            var response = await SendMessageAsync(
                SystemPrompts.MergeAgentSystemPrompt,
                prompt,
                onChunk,
                cancellationToken).ConfigureAwait(false);

            var resolvedContent = ExtractCodeBlock(response) ?? response;
            return new MergeResolution(resolvedContent, $"Refined: {userMessage}", 0.7);
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
            await EnsureClientStartedAsync(cancellationToken).ConfigureAwait(false);

            var prompt = BuildExplanationPrompt(session, startLine, endLine);
            return await SendMessageAsync(
                SystemPrompts.MergeAgentSystemPrompt,
                prompt,
                null,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new AiServiceException("Copilot explanation failed.", ex);
        }
    }

    private async Task EnsureClientStartedAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            return;
        }

        _client = new CopilotClient(_options);
        await _client.StartAsync().ConfigureAwait(false);
    }

    private async Task<string> SendMessageAsync(
        string systemPrompt,
        string userPrompt,
        Action<string>? onChunk,
        CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Client not started.");
        }

        // Create or reuse session
        _currentSession?.DisposeAsync().AsTask().Wait();
        _currentSession = await _client.CreateSessionAsync(new SessionConfig
        {
            Model = _activeModel,
            Streaming = onChunk is not null,
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = systemPrompt
            }
        }).ConfigureAwait(false);

        var responseText = new System.Text.StringBuilder();
        var done = new TaskCompletionSource();

        _currentSession.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    var chunk = delta.Data?.DeltaContent ?? string.Empty;
                    responseText.Append(chunk);
                    onChunk?.Invoke(chunk);
                    break;
                case AssistantMessageEvent msg:
                    if (onChunk is null)
                    {
                        responseText.Append(msg.Data?.Content ?? string.Empty);
                    }
                    break;
                case SessionIdleEvent:
                    done.TrySetResult();
                    break;
                case SessionErrorEvent err:
                    done.TrySetException(new AiServiceException(err.Data?.Message ?? "Session error"));
                    break;
            }
        });

        await _currentSession.SendAsync(new MessageOptions { Prompt = userPrompt }).ConfigureAwait(false);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        try
        {
            await done.Task.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await _currentSession.AbortAsync().ConfigureAwait(false);
            throw;
        }
        catch (OperationCanceledException)
        {
            await _currentSession.AbortAsync().ConfigureAwait(false);
            throw new TimeoutException($"AI request timed out after {DefaultTimeout.TotalSeconds} seconds.");
        }

        return responseText.ToString();
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

    private static string BuildExplanationPrompt(MergeSession session, int startLine, int endLine)
    {
        var mergedContent = session.ConflictFile?.OriginalContent ?? string.Empty;
        var lines = mergedContent.Split('\n');
        var relevantLines = string.Join('\n', lines.Skip(startLine - 1).Take(endLine - startLine + 1));

        return $"Explain the changes between lines {startLine}-{endLine} in this merge conflict context:\n\n" +
               $"Lines {startLine}-{endLine}:\n{relevantLines}\n\n" +
               $"Full conflict context:\n{mergedContent}";
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

    private static string? ExtractSection(string text, string startMarker, string? endMarker)
    {
        var startIndex = text.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return null;
        }

        startIndex += startMarker.Length;

        int endIndex;
        if (endMarker is null)
        {
            endIndex = text.Length;
        }
        else
        {
            endIndex = text.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex < 0)
            {
                endIndex = text.Length;
            }
        }

        return text[startIndex..endIndex].Trim();
    }

    private static string? ExtractCodeBlock(string text)
    {
        const string startFence = "```";
        const string endFence = "```";

        var startIndex = text.IndexOf(startFence, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            return null;
        }

        // Skip the fence and any language identifier
        var contentStart = text.IndexOf('\n', startIndex);
        if (contentStart < 0)
        {
            return null;
        }

        contentStart++;

        var endIndex = text.IndexOf(endFence, contentStart, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            return text[contentStart..].Trim();
        }

        return text[contentStart..endIndex].Trim();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_currentSession is not null)
        {
            await _currentSession.DisposeAsync().ConfigureAwait(false);
            _currentSession = null;
        }

        if (_client is not null)
        {
            await _client.StopAsync().ConfigureAwait(false);
            _client = null;
        }
    }
}
