using System.Text;

namespace AutoMerge.Core.Models;

public sealed record ConflictFile(
    string FilePath,
    string OriginalContent,
    Encoding Encoding,
    LineEnding LineEnding,
    IReadOnlyList<ConflictRegion> Regions);
