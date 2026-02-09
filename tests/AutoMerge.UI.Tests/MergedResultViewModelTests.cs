using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.Core.Services;
using AutoMerge.UI.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AutoMerge.UI.Tests;

public sealed class MergedResultViewModelTests
{
    [Fact]
    public void Navigation_stays_enabled_after_approving_one_conflict()
    {
        var conflictParser = new ConflictMarkerParser();
        var diffCalculator = Substitute.For<IDiffCalculator>();
        diffCalculator.CalculateDiffForNewText(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Array.Empty<LineChange>());

        var viewModel = new MergedResultViewModel(conflictParser, diffCalculator);

        const string mergedWithConflicts =
            "line0\n" +
            "<<<<<<< LOCAL\n" +
            "local-value-1\n" +
            "=======\n" +
            "remote-value-1\n" +
            ">>>>>>> REMOTE\n" +
            "between\n" +
            "<<<<<<< LOCAL\n" +
            "local-value-2\n" +
            "=======\n" +
            "remote-value-2\n" +
            ">>>>>>> REMOTE\n" +
            "line-end\n";

        const string resolvedContent =
            "line0\n" +
            "local-value-1\n" +
            "between\n" +
            "remote-value-2\n" +
            "line-end\n";

        viewModel.SetSourceContents("base", "local", "remote", mergedWithConflicts);

        // Simulate resolving both conflicts in the editor (markers removed).
        viewModel.Content = resolvedContent;

        viewModel.ApprovalItems.Should().HaveCount(2);
        viewModel.UnapprovedCount.Should().Be(2);
        viewModel.CurrentConflictIndex.Should().Be(1);
        viewModel.NextConflictCommand.CanExecute(null).Should().BeTrue();

        // User reviews one conflict and toggles it to approved.
        viewModel.ApprovalItems[0].State = ConflictApprovalState.Approved;

        viewModel.UnapprovedCount.Should().Be(1);
        viewModel.CurrentConflictDisplay.Should().Be("1 / 2");
        viewModel.NextConflictCommand.CanExecute(null).Should().BeTrue();

        viewModel.NextConflictCommand.Execute(null);

        viewModel.CurrentConflictIndex.Should().Be(2);
        viewModel.CurrentConflictDisplay.Should().Be("2 / 2");
        viewModel.PreviousConflictCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Single_conflict_enables_both_navigation_buttons()
    {
        var conflictParser = new ConflictMarkerParser();
        var diffCalculator = Substitute.For<IDiffCalculator>();
        diffCalculator.CalculateDiffForNewText(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Array.Empty<LineChange>());

        var viewModel = new MergedResultViewModel(conflictParser, diffCalculator);

        const string mergedWithOneConflict =
            "line0\n" +
            "<<<<<<< LOCAL\n" +
            "local-value-1\n" +
            "=======\n" +
            "remote-value-1\n" +
            ">>>>>>> REMOTE\n" +
            "line-end\n";

        viewModel.SetSourceContents("base", "local", "remote", mergedWithOneConflict);

        viewModel.ApprovalItems.Should().HaveCount(1);
        viewModel.CurrentConflictIndex.Should().Be(1);

        // Both buttons should be enabled even with a single conflict
        viewModel.NextConflictCommand.CanExecute(null).Should().BeTrue();
        viewModel.PreviousConflictCommand.CanExecute(null).Should().BeTrue();

        // Clicking Next should still work (jumps to the single conflict)
        viewModel.NextConflictCommand.Execute(null);
        viewModel.CurrentConflictIndex.Should().Be(1);
        viewModel.ScrollToLine.Should().BeGreaterThan(0);

        // Clicking Previous should also work (jumps to the single conflict)
        viewModel.PreviousConflictCommand.Execute(null);
        viewModel.CurrentConflictIndex.Should().Be(1);
        viewModel.ScrollToLine.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Load_auto_scrolls_to_first_conflict()
    {
        var conflictParser = new ConflictMarkerParser();
        var diffCalculator = Substitute.For<IDiffCalculator>();
        diffCalculator.CalculateDiffForNewText(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Array.Empty<LineChange>());

        var viewModel = new MergedResultViewModel(conflictParser, diffCalculator);

        const string mergedWithConflicts =
            "line0\n" +
            "line1\n" +
            "line2\n" +
            "<<<<<<< LOCAL\n" +
            "local-value-1\n" +
            "=======\n" +
            "remote-value-1\n" +
            ">>>>>>> REMOTE\n" +
            "line-end\n";

        viewModel.SetSourceContents("base", "local", "remote", mergedWithConflicts);

        // After load, the view should auto-scroll to the first conflict
        viewModel.CurrentConflictIndex.Should().Be(1);
        viewModel.ScrollToLine.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Multiple_conflicts_disable_buttons_at_boundaries()
    {
        var conflictParser = new ConflictMarkerParser();
        var diffCalculator = Substitute.For<IDiffCalculator>();
        diffCalculator.CalculateDiffForNewText(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Array.Empty<LineChange>());

        var viewModel = new MergedResultViewModel(conflictParser, diffCalculator);

        const string mergedWithConflicts =
            "line0\n" +
            "<<<<<<< LOCAL\n" +
            "local-value-1\n" +
            "=======\n" +
            "remote-value-1\n" +
            ">>>>>>> REMOTE\n" +
            "between\n" +
            "<<<<<<< LOCAL\n" +
            "local-value-2\n" +
            "=======\n" +
            "remote-value-2\n" +
            ">>>>>>> REMOTE\n" +
            "line-end\n";

        viewModel.SetSourceContents("base", "local", "remote", mergedWithConflicts);

        viewModel.ApprovalItems.Should().HaveCount(2);
        viewModel.CurrentConflictIndex.Should().Be(1);

        // At first conflict: Previous disabled, Next enabled
        viewModel.PreviousConflictCommand.CanExecute(null).Should().BeFalse();
        viewModel.NextConflictCommand.CanExecute(null).Should().BeTrue();

        // Navigate to last conflict
        viewModel.NextConflictCommand.Execute(null);
        viewModel.CurrentConflictIndex.Should().Be(2);

        // At last conflict: Previous enabled, Next disabled
        viewModel.PreviousConflictCommand.CanExecute(null).Should().BeTrue();
        viewModel.NextConflictCommand.CanExecute(null).Should().BeFalse();
    }
}
