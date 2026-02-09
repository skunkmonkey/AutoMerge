using AutoMerge.Core.Models;

namespace AutoMerge.Core.Abstractions;

public interface IDiffCalculator
{
    IReadOnlyList<LineChange> CalculateDiff(string oldText, string newText);

    /// <summary>
    /// Calculates line-level diff changes mapped to the NEW text's line numbers.
    /// Unlike <see cref="CalculateDiff"/> which produces an inline combined view,
    /// this method returns changes aligned to the actual lines in <paramref name="newText"/>.
    /// Only lines that differ are included in the returned list.
    /// </summary>
    IReadOnlyList<LineChange> CalculateDiffForNewText(string oldText, string newText);
}
