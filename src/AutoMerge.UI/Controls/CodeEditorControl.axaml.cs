using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AutoMerge.Core.Models;

namespace AutoMerge.UI.Controls;

public sealed partial class CodeEditorControl : UserControl
{
    private TextEditor? _editor;
    private bool _isUpdating;
    private bool _isInitialized;

    static CodeEditorControl()
    {
        TextProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.UpdateEditorText(args));
        IsReadOnlyProperty.Changed.AddClassHandler<CodeEditorControl>((control, args) => control.UpdateEditorIsReadOnly(args));
    }

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<CodeEditorControl, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<CodeEditorControl, bool>(nameof(IsReadOnly), true);

    public static readonly StyledProperty<string> SyntaxLanguageProperty =
        AvaloniaProperty.Register<CodeEditorControl, string>(nameof(SyntaxLanguage), string.Empty);

    public static readonly StyledProperty<IReadOnlyList<LineChange>> LineChangesProperty =
        AvaloniaProperty.Register<CodeEditorControl, IReadOnlyList<LineChange>>(nameof(LineChanges), Array.Empty<LineChange>());

    public CodeEditorControl()
    {
        InitializeComponent();
        _editor = this.FindControl<TextEditor>("Editor");
        if (_editor is not null)
        {
            _editor.TextChanged += OnEditorTextChanged;
            // Sync initial values now that _editor is available
            SyncEditorWithProperties();
        }
        _isInitialized = true;
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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SyncEditorWithProperties()
    {
        if (_editor is null)
        {
            return;
        }

        _isUpdating = true;
        _editor.Text = Text ?? string.Empty;
        _editor.IsReadOnly = IsReadOnly;
        _isUpdating = false;
    }

    private void UpdateEditorText(AvaloniaPropertyChangedEventArgs args)
    {
        if (_editor is null || _isUpdating)
        {
            return;
        }

        // If not yet initialized, the sync will happen in constructor
        if (!_isInitialized)
        {
            return;
        }

        _editor.Text = args.NewValue as string ?? string.Empty;
    }

    private void UpdateEditorIsReadOnly(AvaloniaPropertyChangedEventArgs args)
    {
        if (_editor is null || !_isInitialized)
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
        Text = _editor.Text ?? string.Empty;
        _isUpdating = false;
    }
}
