using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using AutoMerge.Core.Models;
using AutoMerge.UI.Services;
using AutoMerge.UI.ViewModels;

namespace AutoMerge.UI.Controls;

public sealed partial class CodeEditorControl : UserControl
{
    private TextEditor? _editor;
    private ScrollViewer? _scrollViewer;
    private DiffLineBackgroundRenderer? _diffRenderer;
    private ConflictMarkerBackgroundRenderer? _conflictRenderer;
    private AutoResolvedBackgroundRenderer? _autoResolvedRenderer;
    private MergedConflictRegionRenderer? _regionBoundaryRenderer;
    private ConflictApprovalMargin? _approvalMargin;
    private FoldingManager? _foldingManager;
    private ConflictMarkerFoldingStrategy? _foldingStrategy;
    private bool _isUpdating;

    static CodeEditorControl()
    {
        TextProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.OnTextPropertyChanged(args));
        IsReadOnlyProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.OnIsReadOnlyPropertyChanged(args));
        LineChangesProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.OnLineChangesPropertyChanged(args));
        ShowConflictMarkersProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.OnShowConflictMarkersPropertyChanged(args));
        ScrollToLineProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.OnScrollToLinePropertyChanged(args));
        AutoResolvedRegionsProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.OnAutoResolvedRegionsPropertyChanged(args));
        ScrollOffsetXProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.OnScrollOffsetXPropertyChanged(args));
        ScrollOffsetYProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.OnScrollOffsetYPropertyChanged(args));
        ConflictApprovalItemsProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.OnConflictApprovalItemsPropertyChanged(args));
    }

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<CodeEditorControl, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<CodeEditorControl, bool>(nameof(IsReadOnly), true);

    public static readonly StyledProperty<string> SyntaxLanguageProperty =
        AvaloniaProperty.Register<CodeEditorControl, string>(nameof(SyntaxLanguage), string.Empty);

    public static readonly StyledProperty<IReadOnlyList<LineChange>> LineChangesProperty =
        AvaloniaProperty.Register<CodeEditorControl, IReadOnlyList<LineChange>>(nameof(LineChanges), Array.Empty<LineChange>());

    public static readonly StyledProperty<bool> ShowConflictMarkersProperty =
        AvaloniaProperty.Register<CodeEditorControl, bool>(nameof(ShowConflictMarkers), false);

    public static readonly StyledProperty<int> ScrollToLineProperty =
        AvaloniaProperty.Register<CodeEditorControl, int>(nameof(ScrollToLine), 0);

    public static readonly StyledProperty<IReadOnlyList<AutoResolvedRegion>> AutoResolvedRegionsProperty =
        AvaloniaProperty.Register<CodeEditorControl, IReadOnlyList<AutoResolvedRegion>>(nameof(AutoResolvedRegions), Array.Empty<AutoResolvedRegion>());

    public static readonly StyledProperty<double> ScrollOffsetXProperty =
        AvaloniaProperty.Register<CodeEditorControl, double>(nameof(ScrollOffsetX), 0.0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> ScrollOffsetYProperty =
        AvaloniaProperty.Register<CodeEditorControl, double>(nameof(ScrollOffsetY), 0.0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyList<ConflictApprovalItem>> ConflictApprovalItemsProperty =
        AvaloniaProperty.Register<CodeEditorControl, IReadOnlyList<ConflictApprovalItem>>(nameof(ConflictApprovalItems), Array.Empty<ConflictApprovalItem>());

    public CodeEditorControl()
    {
        InitializeComponent();
        _editor = this.FindControl<TextEditor>("Editor");
        if (_editor is not null)
        {
            ConfigureEditor(_editor);
            
            // Create and install the diff line background renderer
            _diffRenderer = new DiffLineBackgroundRenderer(_editor.TextArea.TextView);
            _editor.TextArea.TextView.BackgroundRenderers.Add(_diffRenderer);
            
            // Create the conflict marker renderer (added lazily when enabled)
            _conflictRenderer = new ConflictMarkerBackgroundRenderer(_editor.TextArea.TextView);
            
            // Create the auto-resolved background renderer (kept for data-binding compatibility; no-op)
            _autoResolvedRenderer = new AutoResolvedBackgroundRenderer(_editor.TextArea.TextView);
            
            // Create the conflict region boundary renderer (draws top/bottom borders + labels)
            _regionBoundaryRenderer = new MergedConflictRegionRenderer(_editor.TextArea.TextView);
            
            // Create folding manager to hide conflict marker lines
            _foldingManager = FoldingManager.Install(_editor.TextArea);
            _foldingStrategy = new ConflictMarkerFoldingStrategy();
            
            // If ShowConflictMarkers is already true (set in XAML), add renderers now
            if (ShowConflictMarkers)
            {
                _editor.TextArea.TextView.BackgroundRenderers.Add(_conflictRenderer);
                _editor.TextArea.TextView.BackgroundRenderers.Add(_regionBoundaryRenderer);
            }
            
            // Install conflict approval margin (leftmost gutter column)
            _approvalMargin = new ConflictApprovalMargin();
            _approvalMargin.ApprovalToggled += _ => ApprovalToggled?.Invoke(_);
            _editor.TextArea.LeftMargins.Insert(0, _approvalMargin);

            _editor.TextChanged += OnEditorTextChanged;
            _editor.TextArea.TextView.ScrollOffsetChanged += OnEditorScrollOffsetChanged;
        }
    }

    private static void ConfigureEditor(TextEditor editor)
    {
        // Modern dark theme colors
        var backgroundBrush = new SolidColorBrush(Color.Parse("#1A1A1E"));
        var textBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
        
        // Apply to editor and text area
        editor.TextArea.Foreground = textBrush;
        editor.Foreground = textBrush;
        editor.Background = backgroundBrush;
        editor.TextArea.Background = backgroundBrush;
        
        // Configure link color for URLs in code
        editor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Color.Parse("#00B7C3"));
        
        // Selection colors - modern blue highlight
        editor.TextArea.SelectionBrush = new SolidColorBrush(Color.Parse("#264F78"));
        editor.TextArea.SelectionForeground = new SolidColorBrush(Color.Parse("#FFFFFF"));
        
        // Caret color - bright for visibility
        editor.TextArea.Caret.CaretBrush = new SolidColorBrush(Color.Parse("#00B7C3"));
        
        // Set options for better display
        editor.Options.EnableHyperlinks = true;
        editor.Options.EnableEmailHyperlinks = false;
        editor.Options.ShowSpaces = false;
        editor.Options.ShowTabs = false;
        editor.Options.HighlightCurrentLine = true;
        
        // Modern line highlight
        editor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.Parse("#20FFFFFF"));
        editor.TextArea.TextView.CurrentLineBorder = new Pen(new SolidColorBrush(Color.Parse("#30FFFFFF")), 1);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public string SyntaxLanguage
    {
        get => GetValue(SyntaxLanguageProperty);
        set => SetValue(SyntaxLanguageProperty, value);
    }

    public IReadOnlyList<LineChange> LineChanges
    {
        get => GetValue(LineChangesProperty);
        set => SetValue(LineChangesProperty, value);
    }

    public bool ShowConflictMarkers
    {
        get => GetValue(ShowConflictMarkersProperty);
        set => SetValue(ShowConflictMarkersProperty, value);
    }

    public int ScrollToLine
    {
        get => GetValue(ScrollToLineProperty);
        set => SetValue(ScrollToLineProperty, value);
    }

    public IReadOnlyList<AutoResolvedRegion> AutoResolvedRegions
    {
        get => GetValue(AutoResolvedRegionsProperty);
        set => SetValue(AutoResolvedRegionsProperty, value);
    }

    public double ScrollOffsetX
    {
        get => GetValue(ScrollOffsetXProperty);
        set => SetValue(ScrollOffsetXProperty, value);
    }

    public double ScrollOffsetY
    {
        get => GetValue(ScrollOffsetYProperty);
        set => SetValue(ScrollOffsetYProperty, value);
    }

    public IReadOnlyList<ConflictApprovalItem> ConflictApprovalItems
    {
        get => GetValue(ConflictApprovalItemsProperty);
        set => SetValue(ConflictApprovalItemsProperty, value);
    }

    /// <summary>
    /// Raised when the user clicks an approval indicator in the gutter.
    /// The item's <see cref="ConflictApprovalItem.State"/> has already been
    /// toggled by the margin.
    /// </summary>
    public event Action<ConflictApprovalItem>? ApprovalToggled;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnTextPropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (_editor is null || _isUpdating)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            var newText = args.NewValue as string ?? string.Empty;
            _editor.Text = newText;
            
            // Update foldings to hide conflict marker lines
            if (ShowConflictMarkers && _foldingManager is not null && _foldingStrategy is not null)
            {
                _foldingStrategy.UpdateFoldings(_foldingManager, _editor.Document);
                _editor.TextArea.TextView.InvalidateLayer(AvaloniaEdit.Rendering.KnownLayer.Background);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnIsReadOnlyPropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (_editor is null)
        {
            return;
        }

        _editor.IsReadOnly = args.NewValue is bool flag && flag;
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_editor is null || _isUpdating)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            Text = _editor.Text;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnLineChangesPropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (_diffRenderer is null)
        {
            return;
        }

        _diffRenderer.LineChanges = args.NewValue as IReadOnlyList<LineChange> ?? Array.Empty<LineChange>();
    }

    private void OnShowConflictMarkersPropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (_editor is null || _conflictRenderer is null)
        {
            return;
        }

        var showConflictMarkers = args.NewValue is bool value && value;
        var renderers = _editor.TextArea.TextView.BackgroundRenderers;

        if (showConflictMarkers)
        {
            // Add conflict marker renderer if not already present
            if (!renderers.Contains(_conflictRenderer))
            {
                renderers.Add(_conflictRenderer);
            }
            
            // Add region boundary renderer if not already present
            if (_regionBoundaryRenderer is not null && !renderers.Contains(_regionBoundaryRenderer))
            {
                renderers.Add(_regionBoundaryRenderer);
            }
            
            // Apply folding to hide marker lines
            if (_foldingManager is not null && _foldingStrategy is not null)
            {
                _foldingStrategy.UpdateFoldings(_foldingManager, _editor.Document);
            }
        }
        else
        {
            // Remove conflict marker renderer
            renderers.Remove(_conflictRenderer);
            
            // Remove region boundary renderer
            if (_regionBoundaryRenderer is not null)
            {
                renderers.Remove(_regionBoundaryRenderer);
            }
            
            // Clear all foldings
            if (_foldingManager is not null)
            {
                _foldingManager.Clear();
            }
        }

        _editor.TextArea.TextView.InvalidateVisual();
    }

    private void OnScrollToLinePropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (_editor is null)
        {
            return;
        }

        var lineNumber = args.NewValue is int line ? line : 0;
        if (lineNumber > 0 && lineNumber <= _editor.Document.LineCount)
        {
            _editor.ScrollToLine(lineNumber);
            // Also move the caret to that line for visual focus
            var lineInfo = _editor.Document.GetLineByNumber(lineNumber);
            _editor.TextArea.Caret.Offset = lineInfo.Offset;
            _editor.TextArea.Caret.BringCaretToView();
        }
    }

    private void OnAutoResolvedRegionsPropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (_autoResolvedRenderer is null)
        {
            return;
        }

        _autoResolvedRenderer.Regions = args.NewValue as IReadOnlyList<AutoResolvedRegion> ?? Array.Empty<AutoResolvedRegion>();
    }

    private void OnConflictApprovalItemsPropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        var items = args.NewValue as IReadOnlyList<ConflictApprovalItem>
            ?? Array.Empty<ConflictApprovalItem>();

        if (_approvalMargin is not null)
        {
            _approvalMargin.Items = items;
        }

        if (_regionBoundaryRenderer is not null)
        {
            _regionBoundaryRenderer.Items = items;
        }
    }

    private bool _isSyncingScroll;

    /// <summary>
    /// Lazily finds and caches the ScrollViewer inside the TextEditor's visual tree.
    /// Calls ApplyTemplate first to ensure the template has been instantiated.
    /// </summary>
    private ScrollViewer? EnsureScrollViewer()
    {
        if (_scrollViewer is null && _editor is not null)
        {
            _editor.ApplyTemplate();
            _scrollViewer = _editor.GetVisualDescendants()
                .OfType<ScrollViewer>()
                .FirstOrDefault();
        }

        return _scrollViewer;
    }

    /// <summary>
    /// Fired when the user scrolls the editor (mouse wheel, scrollbar drag, etc.).
    /// Converts the pixel offset to a normalised ratio (0–1) and pushes it to the
    /// styled properties so other editors can follow at the proportional position.
    /// </summary>
    private void OnEditorScrollOffsetChanged(object? sender, EventArgs e)
    {
        if (_editor is null || _isSyncingScroll)
            return;

        _isSyncingScroll = true;
        try
        {
            var offset = _editor.TextArea.TextView.ScrollOffset;
            var sv = EnsureScrollViewer();

            if (sv is not null)
            {
                var maxY = Math.Max(0, sv.Extent.Height - sv.Viewport.Height);
                var maxX = Math.Max(0, sv.Extent.Width - sv.Viewport.Width);
                var ratioY = maxY > 0 ? offset.Y / maxY : 0;
                var ratioX = maxX > 0 ? offset.X / maxX : 0;
                ScrollOffsetY = ratioY;
                ScrollOffsetX = ratioX;
            }
            else
            {
                ScrollOffsetX = offset.X;
                ScrollOffsetY = offset.Y;
            }
        }
        finally
        {
            _isSyncingScroll = false;
        }
    }

    /// <summary>
    /// Receives a normalised horizontal ratio from another panel and converts
    /// it to the local pixel offset before scrolling.
    /// The guard flag is cleared asynchronously via Dispatcher.Post so it
    /// survives the deferred layout pass that fires ScrollOffsetChanged.
    /// </summary>
    private void OnScrollOffsetXPropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (_editor is null || _isSyncingScroll)
            return;

        _isSyncingScroll = true;

        var ratio = args.NewValue is double val ? val : 0.0;
        var sv = EnsureScrollViewer();

        if (sv is not null)
        {
            var maxX = Math.Max(0, sv.Extent.Width - sv.Viewport.Width);
            sv.Offset = new Avalonia.Vector(ratio * maxX, sv.Offset.Y);
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => _isSyncingScroll = false,
            Avalonia.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// Receives a normalised vertical ratio from another panel and converts
    /// it to the local pixel offset before scrolling.
    /// The guard flag is cleared asynchronously via Dispatcher.Post so it
    /// survives the deferred layout pass that fires ScrollOffsetChanged.
    /// </summary>
    private void OnScrollOffsetYPropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (_editor is null || _isSyncingScroll)
            return;

        _isSyncingScroll = true;

        var ratio = args.NewValue is double val ? val : 0.0;
        var sv = EnsureScrollViewer();

        if (sv is not null)
        {
            var maxY = Math.Max(0, sv.Extent.Height - sv.Viewport.Height);
            sv.Offset = new Avalonia.Vector(sv.Offset.X, ratio * maxY);
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => _isSyncingScroll = false,
            Avalonia.Threading.DispatcherPriority.Background);
    }
}
