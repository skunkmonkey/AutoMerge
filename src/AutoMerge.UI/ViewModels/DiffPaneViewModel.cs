using AutoMerge.Core.Models;
using AutoMerge.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoMerge.UI.ViewModels;

public sealed partial class DiffPaneViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<LineChange> _lineChanges = Array.Empty<LineChange>();

    [ObservableProperty]
    private string _syntaxLanguage = string.Empty;

    [ObservableProperty]
    private bool _isReadOnly = true;

    public void SetContent(string content, IReadOnlyList<LineChange> changes)
    {
        FileLogger.Log($"DiffPaneViewModel[{Title}].SetContent: {content.Length} chars");
        FileLogger.Log($"DiffPaneViewModel[{Title}] first 50: '{content.Substring(0, Math.Min(50, content.Length))}'");
        Content = content;
        FileLogger.Log($"DiffPaneViewModel[{Title}].Content after set: {Content.Length} chars");
        LineChanges = changes;
    }
}
