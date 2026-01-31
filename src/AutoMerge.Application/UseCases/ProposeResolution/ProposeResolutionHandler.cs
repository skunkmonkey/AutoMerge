using AutoMerge.Application.Events;
using AutoMerge.Application.Services;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Application.UseCases.ProposeResolution;

public sealed class ProposeResolutionHandler
{
    private readonly IAiService _aiService;
    private readonly MergeSessionManager _sessionManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IConfigurationService _configurationService;

    public ProposeResolutionHandler(
        IAiService aiService,
        MergeSessionManager sessionManager,
        IEventAggregator eventAggregator,
        IConfigurationService configurationService)
    {
        _aiService = aiService;
        _sessionManager = sessionManager;
        _eventAggregator = eventAggregator;
        _configurationService = configurationService;
    }

    public async Task<ProposeResolutionResult> ExecuteAsync(
        ProposeResolutionCommand command,
        CancellationToken cancellationToken = default)
    {
        var session = _sessionManager.CurrentSession;
        if (session is null)
        {
            return new ProposeResolutionResult(false, null, "No active session.");
        }

        var preferences = command.Preferences ?? await _configurationService.LoadPreferencesAsync(cancellationToken).ConfigureAwait(false);
        session.SetState(SessionState.Analyzing);

        try
        {
            void OnChunk(string chunk) => _eventAggregator.Publish(new AiStreamingChunkEvent(chunk));

            var resolution = await _aiService.ProposeResolutionAsync(
                session,
                preferences,
                OnChunk,
                cancellationToken).ConfigureAwait(false);

            session.UpdateResolution(resolution);
            session.SetState(SessionState.ResolutionProposed);
            _eventAggregator.Publish(new ResolutionProposedEvent());

            return new ProposeResolutionResult(true, resolution, null);
        }
        catch (Exception ex)
        {
            session.SetState(SessionState.Ready);
            _eventAggregator.Publish(new AiErrorEvent(ex.Message));
            return new ProposeResolutionResult(false, null, ex.Message);
        }
    }
}
