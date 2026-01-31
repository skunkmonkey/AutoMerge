using AutoMerge.Core.Models;

namespace AutoMerge.Core.Abstractions;

public interface IDiffCalculator
{
    IReadOnlyList<LineChange> CalculateDiff(string oldText, string newText);
}
