using System.Globalization;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using AutoMerge.UI.ViewModels;

namespace AutoMerge.UI.Controls;

/// <summary>
/// A custom left-hand margin for the merged-result editor that renders
/// BeyondCompare-style conflict approval indicators.
///
///   Red  ❗ — resolved conflict not yet approved by the user (clickable)
///   Green ✓  — user has approved this resolution (clickable to un-approve)
///   Orange ❗ — unresolved conflict (not clickable – still has markers)
///
/// The Accept button is only enabled when every indicator is green.
/// </summary>
public sealed class ConflictApprovalMargin : AbstractMargin
{
    // ── Colours ──────────────────────────────────────────────────────────
    private static readonly IBrush UnapprovedCircleBrush = new SolidColorBrush(Color.Parse("#60F44336"));
    private static readonly IBrush UnapprovedFgBrush     = new SolidColorBrush(Color.Parse("#F44336"));
    private static readonly IBrush ApprovedCircleBrush   = new SolidColorBrush(Color.Parse("#604CAF50"));
    private static readonly IBrush ApprovedFgBrush       = new SolidColorBrush(Color.Parse("#4CAF50"));
    private static readonly IBrush UnresolvedCircleBrush = new SolidColorBrush(Color.Parse("#60FF9800"));
    private static readonly IBrush UnresolvedFgBrush     = new SolidColorBrush(Color.Parse("#FF9800"));
    private static readonly IBrush BackgroundBrush       = new SolidColorBrush(Color.Parse("#1A1A1E"));

    private static readonly Typeface SymbolTypeface =
        new("Segoe UI", FontStyle.Normal, FontWeight.Bold);

    // ── State ────────────────────────────────────────────────────────────
    private IReadOnlyList<ConflictApprovalItem> _items = Array.Empty<ConflictApprovalItem>();

    /// <summary>
    /// The list of approval items to display. Each item's <see cref="ConflictApprovalItem.StartLine"/>
    /// determines the line at which the indicator is rendered.
    /// </summary>
    public IReadOnlyList<ConflictApprovalItem> Items
    {
        get => _items;
        set
        {
            _items = value ?? Array.Empty<ConflictApprovalItem>();
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Raised after the user toggles an indicator so the view-model can update
    /// aggregate counts (<c>AllConflictsApproved</c>, etc.).
    /// </summary>
    public event Action<ConflictApprovalItem>? ApprovalToggled;

    // ── Layout ───────────────────────────────────────────────────────────
    protected override Size MeasureOverride(Size availableSize)
    {
        // Collapse the margin when there is nothing to show.
        return _items.Count > 0 ? new Size(26, 0) : new Size(0, 0);
    }

    // ── Rendering ────────────────────────────────────────────────────────
    public override void Render(DrawingContext drawingContext)
    {
        var tv = TextView;
        if (tv is null || !tv.VisualLinesValid || _items.Count == 0)
            return;

        drawingContext.FillRectangle(BackgroundBrush,
            new Rect(0, 0, Bounds.Width, Bounds.Height));

        foreach (var visualLine in tv.VisualLines)
        {
            var lineNumber = visualLine.FirstDocumentLine.LineNumber;
            var item = FindItemAtLine(lineNumber);
            if (item is null)
                continue;

            var y  = visualLine.VisualTop - tv.ScrollOffset.Y;
            var h  = visualLine.Height;
            var cx = Bounds.Width / 2;
            var cy = y + h / 2;
            var r  = Math.Min(h * 0.38, 9);

            IBrush circleBrush, fgBrush;
            string symbol;

            switch (item.State)
            {
                case ConflictApprovalState.Approved:
                    circleBrush = ApprovedCircleBrush;
                    fgBrush     = ApprovedFgBrush;
                    symbol      = "✓";
                    break;

                case ConflictApprovalState.Resolved:
                    circleBrush = UnapprovedCircleBrush;
                    fgBrush     = UnapprovedFgBrush;
                    symbol      = "!";
                    break;

                default: // Unresolved
                    circleBrush = UnresolvedCircleBrush;
                    fgBrush     = UnresolvedFgBrush;
                    symbol      = "!";
                    break;
            }

            // Circle background
            drawingContext.DrawEllipse(circleBrush, null,
                new Point(cx, cy), r, r);

            // Symbol text
            var ft = new FormattedText(
                symbol,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                SymbolTypeface,
                11,
                fgBrush);

            drawingContext.DrawText(ft,
                new Point(cx - ft.Width / 2, cy - ft.Height / 2));
        }
    }

    // ── Interaction ──────────────────────────────────────────────────────
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var tv = TextView;
        if (tv is null || _items.Count == 0)
            return;

        var pos = e.GetPosition(this);

        foreach (var visualLine in tv.VisualLines)
        {
            var y = visualLine.VisualTop - tv.ScrollOffset.Y;
            var h = visualLine.Height;

            if (pos.Y < y || pos.Y >= y + h)
                continue;

            var item = FindItemAtLine(visualLine.FirstDocumentLine.LineNumber);
            if (item is null)
                continue;

            if (item.State == ConflictApprovalState.Resolved)
            {
                item.State = ConflictApprovalState.Approved;
                ApprovalToggled?.Invoke(item);
                InvalidateVisual();
            }
            else if (item.State == ConflictApprovalState.Approved)
            {
                item.State = ConflictApprovalState.Resolved;
                ApprovalToggled?.Invoke(item);
                InvalidateVisual();
            }
            // Unresolved items cannot be toggled.

            e.Handled = true;
            break;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var item = FindItemAtPosition(e.GetPosition(this));
        Cursor = item is not null && item.State != ConflictApprovalState.Unresolved
            ? new Cursor(StandardCursorType.Hand)
            : Cursor.Default;
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private ConflictApprovalItem? FindItemAtLine(int lineNumber)
    {
        foreach (var item in _items)
        {
            if (item.StartLine == lineNumber)
                return item;
        }
        return null;
    }

    private ConflictApprovalItem? FindItemAtPosition(Point pos)
    {
        var tv = TextView;
        if (tv is null || !tv.VisualLinesValid)
            return null;

        foreach (var visualLine in tv.VisualLines)
        {
            var y = visualLine.VisualTop - tv.ScrollOffset.Y;
            if (pos.Y >= y && pos.Y < y + visualLine.Height)
                return FindItemAtLine(visualLine.FirstDocumentLine.LineNumber);
        }
        return null;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────
    protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
    {
        if (oldTextView is not null)
            oldTextView.VisualLinesChanged -= OnVisualLinesChanged;

        base.OnTextViewChanged(oldTextView, newTextView);

        if (newTextView is not null)
            newTextView.VisualLinesChanged += OnVisualLinesChanged;
    }

    private void OnVisualLinesChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }
}
