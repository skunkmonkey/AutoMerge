using AutoMerge.Core.Models;

namespace AutoMerge.Core.Abstractions;

public interface IConflictParser
{
    IReadOnlyList<ConflictRegion> Parse(string content);

    bool HasConflictMarkers(string content);
}
