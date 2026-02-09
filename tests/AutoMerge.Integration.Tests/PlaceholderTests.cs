using FluentAssertions;
using Xunit;

namespace AutoMerge.Integration.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void Placeholder()
    {
        true.Should().BeTrue();
    }
}
