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

    /// <summary>
    /// The line number to scroll to when navigating conflicts.
    /// The view subscribes to changes on this property.
    /// </summary>
    [ObservableProperty]
    private int _scrollToLine;

    /// <summary>
    /// Horizontal scroll offset for synchronized scrolling across panels.
    /// </summary>
    [ObservableProperty]
    private double _scrollOffsetX;

    /// <summary>
    /// Vertical scroll offset for synchronized scrolling across panels.
    /// </summary>
    [ObservableProperty]
    private double _scrollOffsetY;

    public void SetContent(string content, IReadOnlyList<LineChange> changes)
    {
        Content = content;
        LineChanges = changes;
    }
}
