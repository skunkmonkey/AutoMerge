using System;
using System.IO;
using AutoMerge.Core.Services;
using FluentAssertions;

namespace AutoMerge.Core.Tests;

public sealed class ConflictMarkerParserTests
{
    private static string GetFixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
    }

    [Xunit.Fact]
    public void Parse_ShouldHandleStandardConflict()
    {
        var parser = new ConflictMarkerParser();
        var content = File.ReadAllText(GetFixturePath("standard-conflict.txt"));

        var regions = parser.Parse(content);

        regions.Should().HaveCount(1);
        var region = regions[0];
        region.StartLine.Should().Be(2);
        region.EndLine.Should().Be(8);
        region.BaseContent.Should().BeNull();
        region.LocalContent.Should().Be("local line 1\nlocal line 2");
        region.RemoteContent.Should().Be("remote line 1\nremote line 2");
    }

    [Xunit.Fact]
    public void Parse_ShouldHandleDiff3Conflict()
    {
        var parser = new ConflictMarkerParser();
        var content = File.ReadAllText(GetFixturePath("diff3-conflict.txt"));

        var regions = parser.Parse(content);

        regions.Should().HaveCount(1);
        var region = regions[0];
        region.StartLine.Should().Be(2);
        region.EndLine.Should().Be(8);
        region.BaseContent.Should().Be("base line");
        region.LocalContent.Should().Be("local line");
        region.RemoteContent.Should().Be("remote line");
    }

    [Xunit.Fact]
    public void Parse_ShouldHandleMultipleConflicts()
    {
        var parser = new ConflictMarkerParser();
        var content = File.ReadAllText(GetFixturePath("multiple-conflicts.txt"));

        var regions = parser.Parse(content);

        regions.Should().HaveCount(2);
    }

    [Xunit.Fact]
    public void Parse_ShouldReturnEmptyListWhenNoConflicts()
    {
        var parser = new ConflictMarkerParser();
        var content = File.ReadAllText(GetFixturePath("no-conflicts.txt"));

        var regions = parser.Parse(content);

        regions.Should().BeEmpty();
    }

    [Xunit.Fact]
    public void Parse_ShouldHandleMarkersAtStartAndEnd()
    {
        var parser = new ConflictMarkerParser();
        var content = File.ReadAllText(GetFixturePath("edge-conflict.txt"));

        var regions = parser.Parse(content);

        regions.Should().HaveCount(1);
        var region = regions[0];
        region.StartLine.Should().Be(1);
        region.EndLine.Should().Be(5);
    }

    [Xunit.Fact]
    public void Parse_ShouldHandleEmptyContent()
    {
        var parser = new ConflictMarkerParser();

        var regions = parser.Parse(string.Empty);

        regions.Should().BeEmpty();
    }

    [Xunit.Fact]
    public void HasConflictMarkers_ShouldDetectMarkers()
    {
        var parser = new ConflictMarkerParser();
        var contentWithMarkers = File.ReadAllText(GetFixturePath("standard-conflict.txt"));
        var contentWithoutMarkers = File.ReadAllText(GetFixturePath("no-conflicts.txt"));

        parser.HasConflictMarkers(contentWithMarkers).Should().BeTrue();
        parser.HasConflictMarkers(contentWithoutMarkers).Should().BeFalse();
    }
}
