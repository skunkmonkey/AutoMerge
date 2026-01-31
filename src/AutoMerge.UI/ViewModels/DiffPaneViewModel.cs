using AutoMerge.Core.Models;
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
        Content = content;
        LineChanges = changes;
    }
}
