using FluentAssertions;

namespace AutoMerge.Application.Tests;

public sealed class PlaceholderTests
{
    [Xunit.Fact]
    public void Placeholder_ShouldPass()
    {
        true.Should().BeTrue();
    }
}
