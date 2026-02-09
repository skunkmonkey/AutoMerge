using AutoMerge.Core.Models;

namespace AutoMerge.Core.Services;

public static class LineEndingDetector
{
    public static LineEnding Detect(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return LineEnding.LF;
        }

        var crlfCount = 0;
        var lfCount = 0;

        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
            {
                crlfCount++;
                i++;
                continue;
            }

            if (content[i] == '\n')
            {
                lfCount++;
            }
        }

        if (crlfCount > 0 && lfCount > 0)
        {
            return LineEnding.Mixed;
        }

        if (crlfCount > 0)
        {
            return LineEnding.CRLF;
        }

        return LineEnding.LF;
    }
}
