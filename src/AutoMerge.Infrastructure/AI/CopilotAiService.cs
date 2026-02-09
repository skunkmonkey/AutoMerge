using System.Globalization;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.Infrastructure.AI.Prompts;
using AutoMerge.Infrastructure.Localization;
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
    private readonly SemaphoreSlim _clientGate = new(1, 1);
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
                return new AiServiceStatus(false, false, InfrastructureStrings.CopilotClientStartFailed);
            }

            var pingResult = await _client.PingAsync().ConfigureAwait(false);

            // Ping succeeds without auth — explicitly check authentication status
            var authStatus = await _client.GetAuthStatusAsync(cancellationToken).ConfigureAwait(false);
            if (authStatus?.IsAuthenticated != true)
            {
                var authMessage = authStatus?.StatusMessage;
                return new AiServiceStatus(true, false,
                    string.IsNullOrWhiteSpace(authMessage)
                        ? InfrastructureStrings.CopilotCliAuthRequired
                        : authMessage);
            }

            return new AiServiceStatus(true, true, null, _activeModel);
        }
        catch (FileNotFoundException)
        {
            return new AiServiceStatus(false, false, InfrastructureStrings.CopilotCliNotFound);
        }
        catch (Exception ex)
        {
            // Check for common authentication issues
            if (ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase))
            {
                return new AiServiceStatus(true, false, InfrastructureStrings.CopilotCliAuthRequired);
            }

            return new AiServiceStatus(false, false,
                string.Format(CultureInfo.CurrentCulture, InfrastructureStrings.CopilotConnectionFailedFormat, ex.Message));
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
            throw new AiServiceException(InfrastructureStrings.CopilotAnalysisFailed, ex);
        }
    }

    public async Task<MergeResolution> ProposeResolutionAsync(
        MergeSession session,
        UserPreferences? preferences = null,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default,
        string? localIntent = null,
        string? remoteIntent = null)
    {
        try
        {
            var prefs = preferences ?? UserPreferences.Default;
            SetModel(prefs.AiModel);
            await EnsureClientStartedAsync(cancellationToken).ConfigureAwait(false);

            var prompt = BuildResolutionPrompt(session, prefs, localIntent, remoteIntent);
            var response = await SendMessageAsync(
                SystemPrompts.MergeAgentSystemPrompt,
                prompt,
                onChunk,
                cancellationToken).ConfigureAwait(false);

            // Extract the resolution content using the required output format
            var resolvedContent = ExtractResolvedContent(response) ?? response;
            var explanation = ExtractSection(response, "Explanation:", null) ?? InfrastructureStrings.AiProposedResolution;

            return new MergeResolution(resolvedContent, explanation, 0.75);
        }
        catch (Exception ex)
        {
            throw new AiServiceException(InfrastructureStrings.CopilotResolutionProposalFailed, ex);
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

            var resolvedContent = ExtractResolvedContent(response) ?? response;
            return new MergeResolution(resolvedContent,
                string.Format(CultureInfo.CurrentCulture, InfrastructureStrings.RefinedMessageFormat, userMessage),
                0.7);
        }
        catch (Exception ex)
        {
            throw new AiServiceException(InfrastructureStrings.CopilotRefinementFailed, ex);
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
            throw new AiServiceException(InfrastructureStrings.CopilotExplanationFailed, ex);
        }
    }

    public async Task<string> ResearchIntentAsync(
        MergeSession session,
        FileVersion version,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureClientStartedAsync(cancellationToken).ConfigureAwait(false);

            var versionLabel = version == FileVersion.Local ? "LOCAL" : "REMOTE";
            var contentPath = version == FileVersion.Local
                ? session.MergeInput.LocalPath
                : session.MergeInput.RemotePath;

            var prompt = SystemPrompts.IntentResearchPromptTemplate
                .Replace("{VERSION}", versionLabel)
                .Replace("{BASE}", SafeReadFile(session.MergeInput.BasePath))
                .Replace("{CONTENT}", SafeReadFile(contentPath));

            // Each intent research uses a fresh session (new context window)
            return await SendMessageInFreshSessionAsync(
                SystemPrompts.MergeAgentSystemPrompt,
                prompt,
                onChunk,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new AiServiceException(
                string.Format(CultureInfo.CurrentCulture, InfrastructureStrings.CopilotIntentResearchFailedFormat, version),
                ex);
        }
    }

    private async Task EnsureClientStartedAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            return;
        }

        await _clientGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is not null)
            {
                return;
            }

            var client = new CopilotClient(_options);
            await client.StartAsync().ConfigureAwait(false);
            _client = client;
        }
        finally
        {
            _clientGate.Release();
        }
    }

    /// <summary>
    /// Verifies the SDK client is authenticated. 
    /// Call after <see cref="EnsureClientStartedAsync"/> and before issuing requests
    /// to fail fast with a clear error instead of silently timing out.
    /// </summary>
    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            throw new InvalidOperationException(InfrastructureStrings.ClientNotStarted);
        }

        var authStatus = await _client.GetAuthStatusAsync(cancellationToken).ConfigureAwait(false);
        if (authStatus?.IsAuthenticated != true)
        {
            throw new AiServiceException(InfrastructureStrings.CopilotCliAuthRequired);
        }
    }

    /// <summary>
    /// Sends a message in a brand-new session (new context window), leaving the main
    /// session (<see cref="_currentSession"/>) untouched. Used for intent research so
    /// each research step gets an isolated context.
    /// </summary>
    private async Task<string> SendMessageInFreshSessionAsync(
        string systemPrompt,
        string userPrompt,
        Action<string>? onChunk,
        CancellationToken cancellationToken)
    {
        // Serialize client-level operations (auth check + session creation)
        // so parallel calls don't race on the JSON-RPC transport.
        CopilotSession session;
        await _clientGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is null)
            {
                throw new InvalidOperationException(InfrastructureStrings.ClientNotStarted);
            }

            await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

            session = await _client.CreateSessionAsync(new SessionConfig
            {
                Model = _activeModel,
                Streaming = onChunk is not null,
                SystemMessage = new SystemMessageConfig
                {
                    Mode = SystemMessageMode.Append,
                    Content = systemPrompt
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            _clientGate.Release();
        }

        try
        {
            var responseText = new System.Text.StringBuilder();
            var done = new TaskCompletionSource();
            var receivedDelta = false;

            session.On(evt =>
            {
                switch (evt)
                {
                    case AssistantMessageDeltaEvent delta:
                        var chunk = delta.Data?.DeltaContent ?? string.Empty;
                        if (chunk.Length > 0)
                        {
                            responseText.Append(chunk);
                            receivedDelta = true;
                            onChunk?.Invoke(chunk);
                        }
                        break;
                    case AssistantMessageEvent msg:
                        var content = msg.Data?.Content ?? string.Empty;
                        if (!string.IsNullOrEmpty(content))
                        {
                            if (!receivedDelta)
                            {
                                responseText.Append(content);
                                onChunk?.Invoke(content);
                            }
                            else if (content.Length > responseText.Length)
                            {
                                responseText.Clear();
                                responseText.Append(content);
                            }
                        }

                        // Always signal completion — AssistantMessageEvent is the
                        // canonical end-of-response regardless of streaming mode.
                        done.TrySetResult();
                        break;
                    case SessionIdleEvent:
                        done.TrySetResult();
                        break;
                    case SessionErrorEvent err:
                        done.TrySetException(new AiServiceException(err.Data?.Message ?? InfrastructureStrings.SessionError));
                        break;
                }
            });

            await session.SendAsync(new MessageOptions { Prompt = userPrompt }).ConfigureAwait(false);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(DefaultTimeout);

            try
            {
                await done.Task.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await session.AbortAsync().ConfigureAwait(false);
                throw;
            }
            catch (OperationCanceledException)
            {
                await session.AbortAsync().ConfigureAwait(false);
                throw new TimeoutException(
                    string.Format(CultureInfo.CurrentCulture, InfrastructureStrings.AiRequestTimeoutFormat, DefaultTimeout.TotalSeconds));
            }

            return responseText.ToString();
        }
        finally
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<string> SendMessageAsync(
        string systemPrompt,
        string userPrompt,
        Action<string>? onChunk,
        CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            throw new InvalidOperationException(InfrastructureStrings.ClientNotStarted);
        }

        await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        // Create or reuse session
        if (_currentSession is not null)
        {
            await _currentSession.DisposeAsync().ConfigureAwait(false);
            _currentSession = null;
        }

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
        var receivedDelta = false;

        _currentSession.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    var chunk = delta.Data?.DeltaContent ?? string.Empty;
                    if (chunk.Length > 0)
                    {
                        responseText.Append(chunk);
                        receivedDelta = true;
                        onChunk?.Invoke(chunk);
                    }
                    break;
                case AssistantMessageEvent msg:
                    var content = msg.Data?.Content ?? string.Empty;
                    if (!string.IsNullOrEmpty(content))
                    {
                        if (!receivedDelta)
                        {
                            responseText.Append(content);
                            onChunk?.Invoke(content);
                        }
                        else if (content.Length > responseText.Length)
                        {
                            responseText.Clear();
                            responseText.Append(content);
                        }
                    }

                    // Always signal completion — AssistantMessageEvent is the
                    // canonical end-of-response regardless of streaming mode.
                    done.TrySetResult();
                    break;
                case SessionIdleEvent:
                    done.TrySetResult();
                    break;
                case SessionErrorEvent err:
                    done.TrySetException(new AiServiceException(err.Data?.Message ?? InfrastructureStrings.SessionError));
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
            throw new TimeoutException(
                string.Format(CultureInfo.CurrentCulture, InfrastructureStrings.AiRequestTimeoutFormat, DefaultTimeout.TotalSeconds));
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
        return BuildResolutionPrompt(session, preferences, null, null);
    }

    private static string BuildResolutionPrompt(MergeSession session, UserPreferences preferences, string? localIntent, string? remoteIntent)
    {
        var mergedContent = session.ConflictFile?.OriginalContent ?? string.Empty;
        var preferenceText = $"DefaultBias: {preferences.DefaultBias}; AutoAnalyzeOnLoad: {preferences.AutoAnalyzeOnLoad}; Theme: {preferences.Theme}";

        // When intent research is available, use the intent-aware template
        if (!string.IsNullOrWhiteSpace(localIntent) && !string.IsNullOrWhiteSpace(remoteIntent))
        {
            return SystemPrompts.IntentAwareResolutionPromptTemplate
                .Replace("{LOCAL_INTENT}", localIntent)
                .Replace("{REMOTE_INTENT}", remoteIntent)
                .Replace("{PREFERENCES}", preferenceText)
                .Replace("{BASE}", SafeReadFile(session.MergeInput.BasePath))
                .Replace("{LOCAL}", SafeReadFile(session.MergeInput.LocalPath))
                .Replace("{REMOTE}", SafeReadFile(session.MergeInput.RemotePath))
                .Replace("{MERGED}", mergedContent);
        }

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

    private static string? ExtractResolvedContent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var labeled = ExtractLabeledResolvedContent(text);
        if (!string.IsNullOrWhiteSpace(labeled))
        {
            return labeled;
        }

        var blocks = ExtractCodeBlocks(text);
        if (blocks.Count == 0)
        {
            return null;
        }

        CodeBlock? best = null;
        var bestScore = int.MinValue;

        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block.Content))
            {
                continue;
            }

            var score = block.Content.Length;
            if (!IsDiffLike(block))
            {
                score += 100_000;
            }

            if (!ContainsConflictMarkers(block.Content))
            {
                score += 100_000;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = block;
            }
        }

        return best?.Content;
    }

    private static string? ExtractLabeledResolvedContent(string text)
    {
        var labelIndex = IndexOfLabel(text, "RESOLVED_CONTENT");
        if (labelIndex < 0)
        {
            labelIndex = IndexOfLabel(text, "RESOLVED CONTENT");
        }

        if (labelIndex < 0)
        {
            return null;
        }

        var afterLabel = text[labelIndex..];
        var blocks = ExtractCodeBlocks(afterLabel);
        if (blocks.Count > 0)
        {
            return blocks[0].Content;
        }

        var colonIndex = afterLabel.IndexOf(':');
        var payload = colonIndex >= 0 ? afterLabel[(colonIndex + 1)..] : afterLabel;

        var explanationIndex = IndexOfLabel(payload, "EXPLANATION");
        if (explanationIndex >= 0)
        {
            payload = payload[..explanationIndex];
        }

        return payload.Trim();
    }

    private static int IndexOfLabel(string text, string label)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(label))
        {
            return -1;
        }

        return text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CodeBlock(string Language, string Content);

    private static IReadOnlyList<CodeBlock> ExtractCodeBlocks(string text)
    {
        var blocks = new List<CodeBlock>();

        var index = 0;
        while (index < text.Length)
        {
            var startIndex = text.IndexOf("```", index, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                break;
            }

            var lineEnd = text.IndexOf('\n', startIndex + 3);
            if (lineEnd < 0)
            {
                break;
            }

            var language = text.Substring(startIndex + 3, lineEnd - (startIndex + 3)).Trim().TrimEnd('\r');
            var contentStart = lineEnd + 1;

            var endIndex = text.IndexOf("```", contentStart, StringComparison.Ordinal);
            if (endIndex < 0)
            {
                var content = text[contentStart..].Trim();
                blocks.Add(new CodeBlock(language, content));
                break;
            }

            var blockContent = text[contentStart..endIndex].Trim();
            blocks.Add(new CodeBlock(language, blockContent));
            index = endIndex + 3;
        }

        return blocks;
    }

    private static bool IsDiffLike(CodeBlock block)
    {
        if (!string.IsNullOrWhiteSpace(block.Language))
        {
            if (string.Equals(block.Language, "diff", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(block.Language, "patch", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var lines = block.Content.Split('\n');
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("diff ", StringComparison.Ordinal) ||
                line.StartsWith("index ", StringComparison.Ordinal) ||
                line.StartsWith("--- ", StringComparison.Ordinal) ||
                line.StartsWith("+++ ", StringComparison.Ordinal) ||
                line.StartsWith("@@ ", StringComparison.Ordinal) ||
                line.StartsWith("@@", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsConflictMarkers(string text)
    {
        return text.Contains("<<<<<<<", StringComparison.Ordinal) ||
               text.Contains("|||||||", StringComparison.Ordinal) ||
               text.Contains("=======", StringComparison.Ordinal) ||
               text.Contains(">>>>>>>", StringComparison.Ordinal);
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
