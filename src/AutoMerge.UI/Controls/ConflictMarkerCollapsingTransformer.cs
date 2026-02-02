using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;

namespace AutoMerge.UI.Controls;

/// <summary>
/// A folding strategy that collapses Git conflict marker lines (<<<<<<, ======, >>>>>>>, |||||||).
/// This makes the markers invisible while preserving the document content.
/// </summary>
public sealed class ConflictMarkerFoldingStrategy
{
    /// <summary>
    /// Creates folding sections for all conflict marker lines in the document.
    /// </summary>
    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var newFoldings = CreateNewFoldings(document);
        manager.UpdateFoldings(newFoldings, -1);
        
        // Collapse all the marker foldings immediately
        foreach (var folding in manager.AllFoldings)
        {
            folding.IsFolded = true;
        }
    }

    private IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
    {
        var foldings = new List<NewFolding>();

        for (int i = 1; i <= document.LineCount; i++)
        {
            var line = document.GetLineByNumber(i);
            var text = document.GetText(line);

            if (IsConflictMarkerLine(text))
            {
                // Create a folding for this single line (including the newline)
                var startOffset = line.Offset;
                var endOffset = line.EndOffset;
                
                // Include the newline character if present
                if (endOffset < document.TextLength)
                {
                    var nextChar = document.GetCharAt(endOffset);
                    if (nextChar == '\r' || nextChar == '\n')
                    {
                        endOffset++;
                        if (endOffset < document.TextLength && document.GetCharAt(endOffset) == '\n')
                        {
                            endOffset++;
                        }
                    }
                }

                if (endOffset > startOffset)
                {
                    foldings.Add(new NewFolding(startOffset, endOffset) 
                    { 
                        Name = string.Empty,
                        DefaultClosed = true
                    });
                }
            }
        }

        // Sort by start offset (required by FoldingManager)
        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return foldings;
    }

    /// <summary>
    /// Checks if a line is a conflict marker line.
    /// </summary>
    public static bool IsConflictMarkerLine(string text)
    {
        var trimmed = text.TrimStart();
        return trimmed.StartsWith("<<<<<<<") ||
               trimmed.StartsWith("|||||||") ||
               trimmed.StartsWith("=======") ||
               trimmed.StartsWith(">>>>>>>");
    }
}
