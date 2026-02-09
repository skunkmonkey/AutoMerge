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
}
