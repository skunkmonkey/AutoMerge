using FluentAssertions;

namespace AutoMerge.Logic.Tests;

public sealed class PlaceholderTests
{
    [Xunit.Fact]
    public void Placeholder_ShouldPass()
    {
        true.Should().BeTrue();
    }
}
