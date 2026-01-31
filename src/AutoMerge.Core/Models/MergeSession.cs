using System;
using System.Collections.Generic;

namespace AutoMerge.Core.Models;

public sealed class MergeSession
{
    private readonly List<ChatMessage> _conversationHistory = new();

    public MergeSession(MergeInput mergeInput)
    {
        MergeInput = mergeInput ?? throw new ArgumentNullException(nameof(mergeInput));
        Id = Guid.NewGuid();
        State = SessionState.Created;
        CurrentMergedContent = string.Empty;
    }

    public Guid Id { get; }
    public MergeInput MergeInput { get; }
    public SessionState State { get; private set; }
    public ConflictFile? ConflictFile { get; private set; }
    public ConflictAnalysis? ConflictAnalysis { get; private set; }
    public MergeResolution? ProposedResolution { get; private set; }
    public IReadOnlyList<ChatMessage> ConversationHistory => _conversationHistory;
    public string CurrentMergedContent { get; private set; }

    public void SetState(SessionState state)
    {
        State = state;
    }

    public void SetConflictFile(ConflictFile conflictFile)
    {
        ConflictFile = conflictFile ?? throw new ArgumentNullException(nameof(conflictFile));
    }

    public void SetAnalysis(ConflictAnalysis analysis)
    {
        ConflictAnalysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
    }

    public void UpdateResolution(MergeResolution resolution)
    {
        ProposedResolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
    }

    public void SetMergedContent(string content)
    {
        CurrentMergedContent = content ?? throw new ArgumentNullException(nameof(content));
    }

    public void AddChatMessage(ChatMessage message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        _conversationHistory.Add(message);
    }
}
