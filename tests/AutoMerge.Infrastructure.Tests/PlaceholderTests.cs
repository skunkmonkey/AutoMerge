using FluentAssertions;

namespace AutoMerge.Infrastructure.Tests;

public sealed class PlaceholderTests
{
    [Xunit.Fact]
    public void Placeholder_ShouldPass()
    {
        true.Should().BeTrue();
    }
}
