using System.Globalization;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using AutoMerge.UI.ViewModels;

namespace AutoMerge.UI.Controls;

/// <summary>
/// Background renderer that draws clear boundaries around each conflict region
/// in the merged result pane. Provides visual demarcation so users can see
/// exactly where each conflict region starts and ends.
///
/// Visual treatment by state:
///   Unresolved — bright orange top/bottom borders + subtle orange region tint
///   Resolved   — blue top/bottom borders + subtle blue region tint
///   Approved   — very subtle gray borders (reviewed, mostly invisible)
/// </summary>
public sealed class MergedConflictRegionRenderer : IBackgroundRenderer
{
    // ── Resolved (needs review) ──────────────────────────────────────
    private static readonly Pen ResolvedTopPen =
        new(new SolidColorBrush(Colors.Transparent), 0);

    private static readonly Pen ResolvedBottomPen =
        new(new SolidColorBrush(Colors.Transparent), 0);

    private static readonly IBrush ResolvedTint =
        new SolidColorBrush(Colors.Transparent);

    // ── Unresolved (conflict markers still present) ──────────────────
    private static readonly Pen UnresolvedTopPen =
        new(new SolidColorBrush(Color.Parse("#FF9800")), 2);

    private static readonly Pen UnresolvedBottomPen =
        new(new SolidColorBrush(Color.Parse("#60FF9800")), 1);

    private static readonly IBrush UnresolvedTint =
        new SolidColorBrush(Color.Parse("#0CFF9800"));

    private static readonly IBrush UnresolvedLabelBg =
        new SolidColorBrush(Color.Parse("#D04E342E"));

    private static readonly IBrush UnresolvedLabelFg =
        new SolidColorBrush(Color.Parse("#FF9800"));

    // ── Approved (user reviewed) ─────────────────────────────────────
    private static readonly Pen ApprovedTopPen =
        new(new SolidColorBrush(Color.Parse("#304CAF50")), 1);

    private static readonly Pen ApprovedBottomPen =
        new(new SolidColorBrush(Color.Parse("#254CAF50")), 1);

    private static readonly Typeface LabelTypeface =
        new("Segoe UI, Noto Sans, sans-serif", FontStyle.Normal, FontWeight.Medium);

    private readonly TextView _textView;
    private IReadOnlyList<ConflictApprovalItem> _items = Array.Empty<ConflictApprovalItem>();

    public MergedConflictRegionRenderer(TextView textView)
    {
        _textView = textView;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public IReadOnlyList<ConflictApprovalItem> Items
    {
        get => _items;
        set
        {
            _items = value ?? Array.Empty<ConflictApprovalItem>();
            _textView.InvalidateLayer(Layer);
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_items.Count == 0)
            return;

        foreach (var item in _items)
        {
            DrawRegion(textView, drawingContext, item);
        }
    }

    private void DrawRegion(TextView textView, DrawingContext drawingContext, ConflictApprovalItem item)
    {
        var (topPen, bottomPen, tint, labelBg, labelFg) = GetBrushes(item.State);
        double? topY = null;
        double? bottomY = null;
        var width = textView.Bounds.Width;

        foreach (var visualLine in textView.VisualLines)
        {
            var lineNumber = visualLine.FirstDocumentLine.LineNumber;
            if (lineNumber < item.StartLine || lineNumber > item.EndLine)
                continue;

            var lineTop = visualLine.VisualTop - textView.ScrollOffset.Y;
            var lineHeight = visualLine.Height;

            // Draw the subtle region tint on every line of the region
            // (skip for Approved — they should be visually quiet)
            if (tint is not null && item.State != ConflictApprovalState.Approved)
            {
                drawingContext.FillRectangle(tint,
                    new Rect(0, lineTop, width, lineHeight));
            }

            if (lineNumber == item.StartLine)
                topY = lineTop;
            if (lineNumber == item.EndLine)
                bottomY = lineTop + lineHeight;
        }

        // ── Draw top border ──────────────────────────────────────────
        if (topY.HasValue)
        {
            drawingContext.DrawLine(topPen,
                new Point(0, topY.Value),
                new Point(width, topY.Value));

            // Draw a region label tag on the top border (Resolved / Unresolved only)
            if (item.State != ConflictApprovalState.Approved && labelBg is not null && labelFg is not null)
            {
                DrawRegionLabel(drawingContext, item, topY.Value, labelBg, labelFg);
            }
        }

        // ── Draw bottom border ───────────────────────────────────────
        if (bottomY.HasValue)
        {
            drawingContext.DrawLine(bottomPen,
                new Point(0, bottomY.Value),
                new Point(width, bottomY.Value));
        }
    }

    private static void DrawRegionLabel(
        DrawingContext dc,
        ConflictApprovalItem item,
        double topY,
        IBrush background,
        IBrush foreground)
    {
        var stateLabel = item.State switch
        {
            ConflictApprovalState.Unresolved => "Needs Resolution",
            ConflictApprovalState.Resolved   => "Resolved — Click ✓ to Approve",
            _                                => string.Empty
        };

        if (string.IsNullOrEmpty(stateLabel))
            return;

        var labelText = string.Format(
            CultureInfo.InvariantCulture,
            "  Conflict {0}  ·  {1}  ",
            item.Index + 1,
            stateLabel);

        var ft = new FormattedText(
            labelText,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            10,
            foreground);

        var labelX = 8.0;
        var labelY = topY - ft.Height / 2;
        var pillRect = new Rect(labelX - 3, labelY - 1.5, ft.Width + 6, ft.Height + 3);

        // Draw pill background
        dc.DrawRectangle(background, null, pillRect, 3, 3);

        // Draw label text
        dc.DrawText(ft, new Point(labelX, labelY));
    }

    private static (Pen Top, Pen Bottom, IBrush? Tint, IBrush? LabelBg, IBrush? LabelFg) GetBrushes(
        ConflictApprovalState state)
    {
        return state switch
        {
            ConflictApprovalState.Unresolved =>
                (UnresolvedTopPen, UnresolvedBottomPen, UnresolvedTint, UnresolvedLabelBg, UnresolvedLabelFg),
            ConflictApprovalState.Resolved =>
                (ResolvedTopPen, ResolvedBottomPen, ResolvedTint, null, null),
            ConflictApprovalState.Approved =>
                (ApprovedTopPen, ApprovedBottomPen, null, null, null),
            _ =>
                (ResolvedTopPen, ResolvedBottomPen, ResolvedTint, null, null)
        };
    }
}
