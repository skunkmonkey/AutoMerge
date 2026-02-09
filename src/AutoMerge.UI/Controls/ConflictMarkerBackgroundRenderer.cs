using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace AutoMerge.UI.Controls;

/// <summary>
/// Background renderer that highlights Git conflict marker sections.
/// Colors the different parts of a conflict like BeyondCompare:
/// - LOCAL/OURS section (after &lt;&lt;&lt;&lt;&lt;&lt;&lt;) = Green
/// - BASE section (after |||||||) = Blue  
/// - REMOTE/THEIRS section (after =======) = Red
/// - Markers themselves = Dimmed
/// </summary>
public sealed class ConflictMarkerBackgroundRenderer : IBackgroundRenderer
{
    // Section background colors — BeyondCompare-inspired softer tones
    private static readonly IBrush LocalBrush = new SolidColorBrush(Color.Parse("#35A5D6A7"));       // Soft green tint
    private static readonly IBrush BaseBrush = new SolidColorBrush(Color.Parse("#3590CAF9"));       // Soft blue tint
    private static readonly IBrush RemoteBrush = new SolidColorBrush(Color.Parse("#35EF9A9A"));     // Soft red/pink tint
    
    // Conflict marker line background (the <<<, ===, >>> lines themselves)
    private static readonly IBrush ConflictMarkerLineBrush = new SolidColorBrush(Color.Parse("#25808080"));  // Dimmed gray
    
    // Left edge marker colors (solid strip, BeyondCompare-style)
    private static readonly IBrush LocalMarkerBrush = new SolidColorBrush(Color.Parse("#4CAF50"));    // Green
    private static readonly IBrush BaseMarkerBrush = new SolidColorBrush(Color.Parse("#42A5F5"));     // Blue
    private static readonly IBrush RemoteMarkerBrush = new SolidColorBrush(Color.Parse("#E53935"));   // Red
    private static readonly IBrush ConflictMarkerLineMarkerBrush = new SolidColorBrush(Color.Parse("#606060"));  // Dim gray

    private readonly TextView _textView;

    public ConflictMarkerBackgroundRenderer(TextView textView)
    {
        _textView = textView;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var document = textView.Document;
        if (document is null)
        {
            return;
        }

        // Parse conflict regions from the document
        var regions = ParseConflictRegions(document);
        
        foreach (var visualLine in textView.VisualLines)
        {
            var lineNumber = visualLine.FirstDocumentLine.LineNumber;
            var lineText = document.GetText(visualLine.FirstDocumentLine);
            
            var (background, marker) = GetBrushesForLine(lineNumber, lineText, regions);
            
            if (background == Brushes.Transparent)
            {
                continue;
            }

            var lineTop = visualLine.VisualTop - textView.ScrollOffset.Y;
            var lineHeight = visualLine.Height;
            var lineWidth = textView.Bounds.Width;
            
            // Draw full line background
            var backgroundRect = new Rect(0, lineTop, lineWidth, lineHeight);
            drawingContext.FillRectangle(background, backgroundRect);
            
            // Draw left edge marker strip (4px)
            var markerRect = new Rect(0, lineTop, 4, lineHeight);
            drawingContext.FillRectangle(marker, markerRect);
        }
    }

    private static List<ConflictRegion> ParseConflictRegions(TextDocument document)
    {
        var regions = new List<ConflictRegion>();
        ConflictRegion? current = null;
        
        for (int i = 1; i <= document.LineCount; i++)
        {
            var line = document.GetLineByNumber(i);
            var text = document.GetText(line);
            
            if (text.StartsWith("<<<<<<<"))
            {
                current = new ConflictRegion { StartLine = i };
            }
            else if (text.StartsWith("|||||||") && current is not null)
            {
                current.BaseSeparatorLine = i;
            }
            else if (text.StartsWith("=======") && current is not null)
            {
                current.MiddleSeparatorLine = i;
            }
            else if (text.StartsWith(">>>>>>>") && current is not null)
            {
                current.EndLine = i;
                regions.Add(current);
                current = null;
            }
        }
        
        return regions;
    }

    private static (IBrush Background, IBrush Marker) GetBrushesForLine(int lineNumber, string lineText, List<ConflictRegion> regions)
    {
        // Check if this line is a conflict marker
        if (lineText.StartsWith("<<<<<<<") || lineText.StartsWith("|||||||") || 
            lineText.StartsWith("=======") || lineText.StartsWith(">>>>>>>"))
        {
            return (ConflictMarkerLineBrush, ConflictMarkerLineMarkerBrush);
        }
        
        // Find which region and section this line belongs to
        foreach (var region in regions)
        {
            if (lineNumber > region.StartLine && lineNumber < region.EndLine)
            {
                // Determine which section of the conflict we're in
                if (region.BaseSeparatorLine.HasValue)
                {
                    // diff3 style with base section
                    if (lineNumber < region.BaseSeparatorLine.Value)
                    {
                        return (LocalBrush, LocalMarkerBrush); // LOCAL/OURS section
                    }
                    else if (lineNumber < region.MiddleSeparatorLine)
                    {
                        return (BaseBrush, BaseMarkerBrush); // BASE section
                    }
                    else
                    {
                        return (RemoteBrush, RemoteMarkerBrush); // REMOTE/THEIRS section
                    }
                }
                else
                {
                    // Standard 2-way conflict
                    if (lineNumber < region.MiddleSeparatorLine)
                    {
                        return (LocalBrush, LocalMarkerBrush); // LOCAL/OURS section
                    }
                    else
                    {
                        return (RemoteBrush, RemoteMarkerBrush); // REMOTE/THEIRS section
                    }
                }
            }
        }
        
        return (Brushes.Transparent, Brushes.Transparent);
    }

    private class ConflictRegion
    {
        public int StartLine { get; set; }        // <<<<<<< line
        public int? BaseSeparatorLine { get; set; } // ||||||| line (diff3 only)
        public int MiddleSeparatorLine { get; set; } // ======= line
        public int EndLine { get; set; }          // >>>>>>> line
    }
}
