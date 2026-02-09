using AutoMerge.Logic.Services;
using AutoMerge.Logic.UseCases.AcceptResolution;
using AutoMerge.Logic.UseCases.CancelMerge;
using AutoMerge.Logic.UseCases.LoadMergeSession;
using AutoMerge.Logic.UseCases.ProposeResolution;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.Core.Services;
using AutoMerge.Infrastructure.AI;
using AutoMerge.Infrastructure.Events;
using AutoMerge.Infrastructure.FileSystem;
using FluentAssertions;
using Xunit;

namespace AutoMerge.Integration.Tests;

public sealed class EndToEndTests
{
    [Fact]
    public async Task LoadSession_parses_conflicts()
    {
        var tempDir = CreateTempDirectory();
        var basePath = CopyFixture(tempDir, "SimpleConflict", "base.txt");
        var localPath = CopyFixture(tempDir, "SimpleConflict", "local.txt");
        var remotePath = CopyFixture(tempDir, "SimpleConflict", "remote.txt");
        var mergedPath = CopyFixture(tempDir, "SimpleConflict", "merged.txt");

        var fileService = new FileService();
        var conflictParser = new ConflictMarkerParser();
        var eventAggregator = new EventAggregator();
        var sessionManager = new MergeSessionManager(eventAggregator);
        var handler = new LoadMergeSessionHandler(fileService, conflictParser, sessionManager, eventAggregator);

        var result = await handler.ExecuteAsync(new LoadMergeSessionCommand(new MergeInput(basePath, localPath, remotePath, mergedPath)));

        result.Success.Should().BeTrue();
        result.Session.Should().NotBeNull();
        result.Session!.ConflictFile!.Regions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Load_propose_accept_writes_output()
    {
        var tempDir = CreateTempDirectory();
        var basePath = CopyFixture(tempDir, "SimpleConflict", "base.txt");
        var localPath = CopyFixture(tempDir, "SimpleConflict", "local.txt");
        var remotePath = CopyFixture(tempDir, "SimpleConflict", "remote.txt");
        var mergedPath = CopyFixture(tempDir, "SimpleConflict", "merged.txt");
        var expectedPath = GetFixturePath("SimpleConflict", "expected.txt");
        var expectedContent = await File.ReadAllTextAsync(expectedPath);

        var fileService = new FileService();
        var conflictParser = new ConflictMarkerParser();
        var eventAggregator = new EventAggregator();
        var sessionManager = new MergeSessionManager(eventAggregator);
        var loadHandler = new LoadMergeSessionHandler(fileService, conflictParser, sessionManager, eventAggregator);
        var loadResult = await loadHandler.ExecuteAsync(new LoadMergeSessionCommand(new MergeInput(basePath, localPath, remotePath, mergedPath)));
        loadResult.Success.Should().BeTrue();

        var mockAi = new MockAiService(resolution: new MergeResolution(expectedContent, "ok", 0.9));
        var proposeHandler = new ProposeResolutionHandler(mockAi, sessionManager, eventAggregator, new InMemoryConfigurationService());
        var proposeResult = await proposeHandler.ExecuteAsync(new ProposeResolutionCommand());
        proposeResult.Success.Should().BeTrue();

        var autoSaveService = new AutoSaveService(fileService);
        var acceptHandler = new AcceptResolutionHandler(fileService, conflictParser, sessionManager, autoSaveService, eventAggregator);
        var acceptResult = await acceptHandler.ExecuteAsync(new AcceptResolutionCommand(proposeResult.Resolution!.ResolvedContent));

        acceptResult.Success.Should().BeTrue();
        var written = await File.ReadAllTextAsync(mergedPath);
        written.Should().Be(expectedContent);
    }

    [Fact]
    public void Cancel_does_not_write_output()
    {
        var tempDir = CreateTempDirectory();
        var basePath = CopyFixture(tempDir, "NoConflict", "base.txt");
        var localPath = CopyFixture(tempDir, "NoConflict", "local.txt");
        var remotePath = CopyFixture(tempDir, "NoConflict", "remote.txt");
        var outputPath = Path.Combine(tempDir, "output.txt");

        var eventAggregator = new EventAggregator();
        var sessionManager = new MergeSessionManager(eventAggregator);
        sessionManager.CreateSession(new MergeInput(basePath, localPath, remotePath, outputPath));

        var cancelHandler = new CancelMergeHandler(sessionManager, new AutoSaveService(new FileService()), eventAggregator);
        cancelHandler.Execute();

        File.Exists(outputPath).Should().BeFalse();
    }

    [Fact]
    public async Task Accept_rejects_conflict_markers()
    {
        var tempDir = CreateTempDirectory();
        var basePath = CopyFixture(tempDir, "SimpleConflict", "base.txt");
        var localPath = CopyFixture(tempDir, "SimpleConflict", "local.txt");
        var remotePath = CopyFixture(tempDir, "SimpleConflict", "remote.txt");
        var outputPath = Path.Combine(tempDir, "output.txt");

        var fileService = new FileService();
        var conflictParser = new ConflictMarkerParser();
        var eventAggregator = new EventAggregator();
        var sessionManager = new MergeSessionManager(eventAggregator);
        var loadHandler = new LoadMergeSessionHandler(fileService, conflictParser, sessionManager, eventAggregator);
        var loadResult = await loadHandler.ExecuteAsync(new LoadMergeSessionCommand(new MergeInput(basePath, localPath, remotePath, outputPath)));
        loadResult.Success.Should().BeTrue();

        var autoSaveService = new AutoSaveService(fileService);
        var acceptHandler = new AcceptResolutionHandler(fileService, conflictParser, sessionManager, autoSaveService, eventAggregator);

        var badContent = "<<<<<<< LOCAL\nconflict\n=======\nother\n>>>>>>> REMOTE\n";
        var result = await acceptHandler.ExecuteAsync(new AcceptResolutionCommand(badContent));

        result.Success.Should().BeFalse();
        File.Exists(outputPath).Should().BeFalse();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AutoMerge.Integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CopyFixture(string tempDir, string fixtureName, string fileName)
    {
        var source = GetFixturePath(fixtureName, fileName);
        var destination = Path.Combine(tempDir, fileName);
        File.Copy(source, destination, true);
        return destination;
    }

    private static string GetFixturePath(string fixtureName, string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName, fileName);
    }

    private sealed class InMemoryConfigurationService : IConfigurationService
    {
        public Task<UserPreferences> LoadPreferencesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(UserPreferences.Default);
        }

        public Task<IReadOnlyList<string>> LoadAiModelOptionsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<string>>(new[] { UserPreferences.Default.AiModel });
        }

        public Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ResetPreferencesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
