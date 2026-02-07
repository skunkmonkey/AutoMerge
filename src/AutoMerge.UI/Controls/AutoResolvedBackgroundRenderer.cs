using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using AutoMerge.Core.Models;

namespace AutoMerge.UI.Controls;

/// <summary>
/// Renders a subtle green/teal background for lines that were auto-resolved
/// by the deterministic three-way merge logic, distinguishing them from
/// remaining unresolved conflicts.
/// </summary>
public sealed class AutoResolvedBackgroundRenderer : IBackgroundRenderer
{
    private static readonly IBrush ResolvedBrush = new SolidColorBrush(Color.Parse("#204CAF50"));
    private static readonly IBrush ResolvedMarkerBrush = new SolidColorBrush(Color.Parse("#4CAF50"));

    private readonly TextView _textView;
    private IReadOnlyList<AutoResolvedRegion> _regions = Array.Empty<AutoResolvedRegion>();

    public AutoResolvedBackgroundRenderer(TextView textView)
    {
        _textView = textView;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public IReadOnlyList<AutoResolvedRegion> Regions
    {
        get => _regions;
        set
        {
            _regions = value ?? Array.Empty<AutoResolvedRegion>();
            _textView.InvalidateLayer(Layer);
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_regions.Count == 0)
        {
            return;
        }

        foreach (var visualLine in textView.VisualLines)
        {
            var lineNumber = visualLine.FirstDocumentLine.LineNumber;

            var isAutoResolved = false;
            foreach (var region in _regions)
            {
                if (lineNumber >= region.StartLine && lineNumber <= region.EndLine)
                {
                    isAutoResolved = true;
                    break;
                }
            }

            if (!isAutoResolved)
            {
                continue;
            }

            var lineTop = visualLine.VisualTop - textView.ScrollOffset.Y;
            var lineHeight = visualLine.Height;
            var lineWidth = textView.Bounds.Width;

            var backgroundRect = new Rect(0, lineTop, lineWidth, lineHeight);
            drawingContext.FillRectangle(ResolvedBrush, backgroundRect);

            var markerRect = new Rect(0, lineTop, 4, lineHeight);
            drawingContext.FillRectangle(ResolvedMarkerBrush, markerRect);
        }
    }
}
