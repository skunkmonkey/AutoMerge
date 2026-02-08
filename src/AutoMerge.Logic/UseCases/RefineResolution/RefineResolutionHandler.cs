using AutoMerge.Logic.Events;
using AutoMerge.Logic.Localization;
using AutoMerge.Logic.Services;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.RefineResolution;

public sealed class RefineResolutionHandler
{
    private readonly IAiService _aiService;
    private readonly MergeSessionManager _sessionManager;
    private readonly IEventAggregator _eventAggregator;

    public RefineResolutionHandler(
        IAiService aiService,
        MergeSessionManager sessionManager,
        IEventAggregator eventAggregator)
    {
        _aiService = aiService;
        _sessionManager = sessionManager;
        _eventAggregator = eventAggregator;
    }

    public async Task<RefineResolutionResult> ExecuteAsync(
        RefineResolutionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var session = _sessionManager.CurrentSession;
        if (session is null)
        {
            return new RefineResolutionResult(false, null, LogicStrings.NoActiveSession);
        }

        session.AddChatMessage(new ChatMessage(ChatRole.User, command.UserMessage, DateTimeOffset.UtcNow));
        session.SetState(SessionState.Refining);

        try
        {
            void OnChunk(string chunk) => _eventAggregator.Publish(new AiStreamingChunkEvent(chunk));

            var resolution = await _aiService.RefineResolutionAsync(
                session,
                command.UserMessage,
                OnChunk,
                cancellationToken).ConfigureAwait(false);

            session.AddChatMessage(new ChatMessage(ChatRole.Assistant, resolution.Explanation, DateTimeOffset.UtcNow));
            session.UpdateResolution(resolution);
            session.SetState(SessionState.ResolutionProposed);

            return new RefineResolutionResult(true, resolution, null);
        }
        catch (Exception ex)
        {
            session.SetState(SessionState.Ready);
            _eventAggregator.Publish(new AiErrorEvent(ex.Message));
            return new RefineResolutionResult(false, null, ex.Message);
        }
    }
}
