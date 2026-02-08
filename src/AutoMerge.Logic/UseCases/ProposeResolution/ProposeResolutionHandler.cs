using AutoMerge.Logic.Events;
using AutoMerge.Logic.Localization;
using AutoMerge.Logic.Services;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.ProposeResolution;

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
            return new ProposeResolutionResult(false, null, LogicStrings.NoActiveSession);
        }

        var preferences = command.Preferences ?? await _configurationService.LoadPreferencesAsync(cancellationToken).ConfigureAwait(false);
        session.SetState(SessionState.Analyzing);

        try
        {
            // ── Step 1: Research Local intent (new context window) ──
            command.OnBusyMessageChanged?.Invoke(LogicStrings.BusyResearchLocalTitle, LogicStrings.BusyResearchLocalMessage);

            var localIntent = await _aiService.ResearchIntentAsync(
                session,
                FileVersion.Local,
                command.OnLocalIntentChunk,
                cancellationToken).ConfigureAwait(false);

            // ── Step 2: Research Remote intent (new context window) ──
            command.OnBusyMessageChanged?.Invoke(LogicStrings.BusyResearchRemoteTitle, LogicStrings.BusyResearchRemoteMessage);

            var remoteIntent = await _aiService.ResearchIntentAsync(
                session,
                FileVersion.Remote,
                command.OnRemoteIntentChunk,
                cancellationToken).ConfigureAwait(false);

            // ── Step 3: Propose resolution with both intents as context (new context window) ──
            command.OnBusyMessageChanged?.Invoke(LogicStrings.BusyResolvingTitle, LogicStrings.BusyResolvingMessage);

            void OnChunk(string chunk) => _eventAggregator.Publish(new AiStreamingChunkEvent(chunk));

            var resolution = await _aiService.ProposeResolutionAsync(
                session,
                preferences,
                OnChunk,
                cancellationToken,
                localIntent,
                remoteIntent).ConfigureAwait(false);

            session.UpdateResolution(resolution);
            session.SetState(SessionState.ResolutionProposed);
            _eventAggregator.Publish(new ResolutionProposedEvent());

            return new ProposeResolutionResult(true, resolution, null, localIntent, remoteIntent);
        }
        catch (Exception ex)
        {
            session.SetState(SessionState.Ready);
            _eventAggregator.Publish(new AiErrorEvent(ex.Message));
            return new ProposeResolutionResult(false, null, ex.Message);
        }
    }
}
