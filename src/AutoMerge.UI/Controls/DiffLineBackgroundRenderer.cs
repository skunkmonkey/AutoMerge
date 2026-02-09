using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using AutoMerge.Core.Models;

namespace AutoMerge.UI.Controls;

/// <summary>
/// Custom background renderer that highlights lines based on diff status.
/// Similar to BeyondCompare-style diff visualization with color-coded backgrounds.
/// </summary>
public sealed class DiffLineBackgroundRenderer : IBackgroundRenderer
{
    // BeyondCompare-inspired colors — softer backgrounds with solid marker strips
    private static readonly IBrush AddedBrush = new SolidColorBrush(Color.Parse("#30A5D6A7"));       // Soft green tint
    private static readonly IBrush RemovedBrush = new SolidColorBrush(Color.Parse("#30EF9A9A"));     // Soft red/pink tint
    private static readonly IBrush ModifiedBrush = new SolidColorBrush(Color.Parse("#30FFE082"));    // Soft amber tint
    
    // Left-side marker strip colors (solid, BeyondCompare-style)
    private static readonly IBrush AddedMarkerBrush = new SolidColorBrush(Color.Parse("#4CAF50"));    // Green
    private static readonly IBrush RemovedMarkerBrush = new SolidColorBrush(Color.Parse("#E53935")); // Red
    private static readonly IBrush ModifiedMarkerBrush = new SolidColorBrush(Color.Parse("#FB8C00")); // Orange

    private readonly TextView _textView;
    private IReadOnlyList<LineChange> _lineChanges = Array.Empty<LineChange>();
    private Dictionary<int, LineChangeType> _lineChangeMap = new();

    public DiffLineBackgroundRenderer(TextView textView)
    {
        _textView = textView;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public IReadOnlyList<LineChange> LineChanges
    {
        get => _lineChanges;
        set
        {
            _lineChanges = value ?? Array.Empty<LineChange>();
            RebuildLineChangeMap();
            _textView.InvalidateLayer(Layer);
        }
    }

    private void RebuildLineChangeMap()
    {
        _lineChangeMap.Clear();
        foreach (var change in _lineChanges)
        {
            _lineChangeMap[change.LineNumber] = change.ChangeType;
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_lineChangeMap.Count == 0)
        {
            return;
        }

        foreach (var visualLine in textView.VisualLines)
        {
            var lineNumber = visualLine.FirstDocumentLine.LineNumber;
            
            if (!_lineChangeMap.TryGetValue(lineNumber, out var changeType) || changeType == LineChangeType.Unchanged)
            {
                continue;
            }

            var (backgroundBrush, markerBrush) = GetBrushesForChangeType(changeType);
            
            // Calculate the line's visual bounds
            var lineTop = visualLine.VisualTop - textView.ScrollOffset.Y;
            var lineHeight = visualLine.Height;
            var lineWidth = textView.Bounds.Width;
            
            // Draw the full line background
            var backgroundRect = new Rect(0, lineTop, lineWidth, lineHeight);
            drawingContext.FillRectangle(backgroundBrush, backgroundRect);
            
            // Draw a colored marker strip on the left edge (4px wide)
            var markerRect = new Rect(0, lineTop, 4, lineHeight);
            drawingContext.FillRectangle(markerBrush, markerRect);
        }
    }

    private static (IBrush Background, IBrush Marker) GetBrushesForChangeType(LineChangeType changeType)
    {
        return changeType switch
        {
            LineChangeType.Added => (AddedBrush, AddedMarkerBrush),
            LineChangeType.Removed => (RemovedBrush, RemovedMarkerBrush),
            LineChangeType.Modified => (ModifiedBrush, ModifiedMarkerBrush),
            _ => (Brushes.Transparent, Brushes.Transparent)
        };
    }
}
