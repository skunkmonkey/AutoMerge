using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using AutoMerge.Core.Models;
using AutoMerge.UI.Services;

namespace AutoMerge.UI.Controls;

public sealed partial class CodeEditorControl : UserControl
{
    private TextEditor? _editor;
    private DiffLineBackgroundRenderer? _diffRenderer;
    private ConflictMarkerBackgroundRenderer? _conflictRenderer;
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

    public CodeEditorControl()
    {
        FileLogger.Log($"CodeEditorControl constructor called");
        InitializeComponent();
        _editor = this.FindControl<TextEditor>("Editor");
        FileLogger.Log($"CodeEditorControl: _editor found = {_editor is not null}");
        if (_editor is not null)
        {
            ConfigureEditor(_editor);
            
            // Create and install the diff line background renderer
            _diffRenderer = new DiffLineBackgroundRenderer(_editor.TextArea.TextView);
            _editor.TextArea.TextView.BackgroundRenderers.Add(_diffRenderer);
            
            // Create the conflict marker renderer (added lazily when enabled)
            _conflictRenderer = new ConflictMarkerBackgroundRenderer(_editor.TextArea.TextView);
            
            // Create folding manager to hide conflict marker lines
            _foldingManager = FoldingManager.Install(_editor.TextArea);
            _foldingStrategy = new ConflictMarkerFoldingStrategy();
            
            // If ShowConflictMarkers is already true (set in XAML), add the renderer now
            if (ShowConflictMarkers)
            {
                _editor.TextArea.TextView.BackgroundRenderers.Add(_conflictRenderer);
            }
            
            _editor.TextChanged += OnEditorTextChanged;
            FileLogger.Log($"CodeEditorControl: Using existing Document, TextChanged subscribed");
        }
        else
        {
            FileLogger.Log($"ERROR: CodeEditorControl._editor is null!");
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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnTextPropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        FileLogger.Log($"CodeEditorControl.OnTextPropertyChanged called. _editor={_editor is not null}, _isUpdating={_isUpdating}");
        if (_editor is null || _isUpdating)
        {
            FileLogger.Log($"CodeEditorControl.OnTextPropertyChanged: SKIPPING (editor null or updating)");
            return;
        }

        _isUpdating = true;
        try
        {
            var newText = args.NewValue as string ?? string.Empty;
            FileLogger.Log($"CodeEditorControl.OnTextPropertyChanged: Setting Text to {newText.Length} chars");
            FileLogger.Log($"CodeEditorControl first 50: '{newText.Substring(0, Math.Min(50, newText.Length))}'");
            _editor.Text = newText;
            FileLogger.Log($"CodeEditorControl.OnTextPropertyChanged: Text now = {_editor.Text.Length} chars");
            
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
}
