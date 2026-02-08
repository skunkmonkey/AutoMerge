using System.Collections.ObjectModel;
using AutoMerge.Logic.Events;
using AutoMerge.Logic.UseCases.RefineResolution;
using AutoMerge.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoMerge.UI.ViewModels;

public sealed partial class AiChatViewModel : ViewModelBase
{
    private readonly RefineResolutionHandler _refineHandler;
    private readonly IDisposable _streamSubscription;

    public AiChatViewModel(RefineResolutionHandler refineHandler, IEventAggregator eventAggregator)
    {
        _refineHandler = refineHandler;
        Messages = new ObservableCollection<ChatMessage>();
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSendMessage);
        ClearHistoryCommand = new RelayCommand(ClearHistory);

        _streamSubscription = eventAggregator.Subscribe<AiStreamingChunkEvent>(evt =>
        {
            StreamingText += evt.ChunkText;
        });
    }

    public ObservableCollection<ChatMessage> Messages { get; }

    [ObservableProperty]
    private string _currentInput = string.Empty;

    [ObservableProperty]
    private bool _isAiResponding;

    [ObservableProperty]
    private string _streamingText = string.Empty;

    public IAsyncRelayCommand SendMessageCommand { get; }
    public IRelayCommand ClearHistoryCommand { get; }

    private bool CanSendMessage()
    {
        return !IsAiResponding && !string.IsNullOrWhiteSpace(CurrentInput);
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentInput))
        {
            return;
        }

        var userMessage = CurrentInput;
        Messages.Add(new ChatMessage(ChatRole.User, userMessage, DateTimeOffset.UtcNow));
        CurrentInput = string.Empty;
        StreamingText = string.Empty;
        IsAiResponding = true;
        SendMessageCommand.NotifyCanExecuteChanged();

        var result = await _refineHandler.ExecuteAsync(new RefineResolutionCommand(userMessage)).ConfigureAwait(false);
        if (result.Success && result.Resolution is not null)
        {
            Messages.Add(new ChatMessage(ChatRole.Assistant, result.Resolution.Explanation, DateTimeOffset.UtcNow));
        }

        IsAiResponding = false;
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    private void ClearHistory()
    {
        Messages.Clear();
        StreamingText = string.Empty;
    }
}
