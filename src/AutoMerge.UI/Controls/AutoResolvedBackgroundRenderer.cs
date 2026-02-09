using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using AutoMerge.Core.Models;

namespace AutoMerge.UI.Controls;

/// <summary>
/// Previously rendered a full green background tint for auto-resolved regions.
/// Now replaced by diff-based per-line highlighting (DiffLineBackgroundRenderer)
/// combined with region boundaries (MergedConflictRegionRenderer).
/// Kept as a no-op to avoid breaking existing data-binding wiring.
/// </summary>
public sealed class AutoResolvedBackgroundRenderer : IBackgroundRenderer
{
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
            // No visual invalidation needed — rendering is a no-op.
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        // Intentionally empty.
        // Region boundaries and per-line diff highlighting now handle
        // all visual treatment for resolved conflict regions.
    }
}
