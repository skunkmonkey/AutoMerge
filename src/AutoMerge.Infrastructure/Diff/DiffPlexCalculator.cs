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
}
