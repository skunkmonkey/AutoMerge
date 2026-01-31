using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Core.Services;

public sealed class ConflictMarkerParser : IConflictParser
{
    private const string StartMarker = "<<<<<<<";
    private const string BaseMarker = "|||||||";
    private const string SeparatorMarker = "=======";
    private const string EndMarker = ">>>>>>>";

    public IReadOnlyList<ConflictRegion> Parse(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return Array.Empty<ConflictRegion>();
        }

        var lines = content.Split('\n');
        var regions = new List<ConflictRegion>();

        var index = 0;
        while (index < lines.Length)
        {
            var line = TrimLineEnding(lines[index]);
            if (!line.StartsWith(StartMarker, StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var startLine = index + 1;
            index++;

            var localLines = new List<string>();
            var baseLines = new List<string>();
            var remoteLines = new List<string>();
            var hasBase = false;

            while (index < lines.Length && !TrimLineEnding(lines[index]).StartsWith(BaseMarker, StringComparison.Ordinal) &&
                   !TrimLineEnding(lines[index]).StartsWith(SeparatorMarker, StringComparison.Ordinal))
            {
                localLines.Add(TrimLineEnding(lines[index]));
                index++;
            }

            if (index < lines.Length && TrimLineEnding(lines[index]).StartsWith(BaseMarker, StringComparison.Ordinal))
            {
                hasBase = true;
                index++;

                while (index < lines.Length && !TrimLineEnding(lines[index]).StartsWith(SeparatorMarker, StringComparison.Ordinal))
                {
                    baseLines.Add(TrimLineEnding(lines[index]));
                    index++;
                }
            }

            if (index < lines.Length && TrimLineEnding(lines[index]).StartsWith(SeparatorMarker, StringComparison.Ordinal))
            {
                index++;
            }

            while (index < lines.Length && !TrimLineEnding(lines[index]).StartsWith(EndMarker, StringComparison.Ordinal))
            {
                remoteLines.Add(TrimLineEnding(lines[index]));
                index++;
            }

            if (index >= lines.Length)
            {
                break;
            }

            var endLine = index + 1;
            var region = new ConflictRegion(
                startLine,
                endLine,
                hasBase ? JoinLines(baseLines) : null,
                JoinLines(localLines),
                JoinLines(remoteLines));

            regions.Add(region);
            index++;
        }

        return regions;
    }

    public bool HasConflictMarkers(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        return content.Contains(StartMarker, StringComparison.Ordinal) ||
               content.Contains(SeparatorMarker, StringComparison.Ordinal) ||
               content.Contains(EndMarker, StringComparison.Ordinal);
    }

    private static string TrimLineEnding(string line)
    {
        return line.TrimEnd('\r');
    }

    private static string JoinLines(List<string> lines)
    {
        return string.Join("\n", lines);
    }
}
