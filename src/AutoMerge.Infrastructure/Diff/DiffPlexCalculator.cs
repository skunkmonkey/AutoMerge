using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace AutoMerge.Infrastructure.Diff;

public sealed class DiffPlexCalculator : IDiffCalculator
{
    public IReadOnlyList<LineChange> CalculateDiff(string oldText, string newText)
    {
        var builder = new InlineDiffBuilder(new Differ());
        var model = builder.BuildDiffModel(oldText ?? string.Empty, newText ?? string.Empty);

        var changes = new List<LineChange>(model.Lines.Count);
        for (var i = 0; i < model.Lines.Count; i++)
        {
            var line = model.Lines[i];
            changes.Add(new LineChange(i + 1, MapType(line.Type), line.Text ?? string.Empty));
        }

        return changes;
    }

    /// <inheritdoc />
    public IReadOnlyList<LineChange> CalculateDiffForNewText(string oldText, string newText)
    {
        var normalizedOld = NormalizeLineEndings(oldText ?? string.Empty);
        var normalizedNew = NormalizeLineEndings(newText ?? string.Empty);

        if (string.IsNullOrEmpty(normalizedNew))
            return Array.Empty<LineChange>();

        var builder = new InlineDiffBuilder(new Differ());
        var model = builder.BuildDiffModel(normalizedOld, normalizedNew);

        var changes = new List<LineChange>();
        var newLineNumber = 0;
        var pendingDeletes = 0;

        foreach (var line in model.Lines)
        {
            if (line.Type == ChangeType.Deleted)
            {
                pendingDeletes++;
                continue;
            }

            if (line.Type == ChangeType.Imaginary)
                continue;

            newLineNumber++;

            if (line.Type == ChangeType.Inserted)
            {
                // If there were preceding deletions, this insertion replaces
                // old text → Modified; otherwise it's purely new → Added.
                var changeType = pendingDeletes > 0
                    ? LineChangeType.Modified
                    : LineChangeType.Added;

                if (pendingDeletes > 0)
                    pendingDeletes--;

                changes.Add(new LineChange(newLineNumber, changeType, line.Text ?? string.Empty));
            }
            else if (line.Type == ChangeType.Modified)
            {
                pendingDeletes = 0;
                changes.Add(new LineChange(newLineNumber, LineChangeType.Modified, line.Text ?? string.Empty));
            }
            else
            {
                // Unchanged — reset pending state
                pendingDeletes = 0;
            }
        }

        return changes;
    }

    private static LineChangeType MapType(ChangeType type)
    {
        return type switch
        {
            ChangeType.Unchanged => LineChangeType.Unchanged,
            ChangeType.Inserted => LineChangeType.Added,
            ChangeType.Deleted => LineChangeType.Removed,
            ChangeType.Modified => LineChangeType.Modified,
            _ => LineChangeType.Modified
        };
    }

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
