using System.Text;
using AutoMerge.Logic.Services;
using AutoMerge.Logic.UseCases.AcceptResolution;
using AutoMerge.Logic.UseCases.CancelMerge;
using AutoMerge.Logic.UseCases.LoadMergeSession;
using AutoMerge.Logic.UseCases.ProposeResolution;
using AutoMerge.Logic.UseCases.RefineResolution;
using AutoMerge.Logic.UseCases.SavePreferences;
using AutoMerge.Logic.UseCases.LoadPreferences;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.Core.Services;
using AutoMerge.Infrastructure.AI;
using AutoMerge.Infrastructure.Events;
using AutoMerge.UI.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AutoMerge.UI.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task Initialize_populates_panes_and_state()
    {
        var context = CreateContext(withConflictMarkers: true);

        await context.ViewModel.InitializeAsync(context.MergeInput);
        if (context.ViewModel.AutoResolveTask is not null)
            await context.ViewModel.AutoResolveTask;

        context.ViewModel.State.Should().Be(SessionState.Ready);
        context.ViewModel.BasePaneViewModel.Content.Should().Be(context.BaseContent);
        context.ViewModel.LocalPaneViewModel.Content.Should().Be(context.LocalContent);
        context.ViewModel.RemotePaneViewModel.Content.Should().Be(context.RemoteContent);
        // AI auto-resolves on load, so merged content is the mock AI result
        context.ViewModel.MergedResultViewModel.Content.Should().NotBeNullOrEmpty();
        context.ViewModel.HasAiResolved.Should().BeTrue();
        context.ViewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task Accept_command_enabled_after_ai_auto_resolves()
    {
        var context = CreateContext(withConflictMarkers: true);

        await context.ViewModel.InitializeAsync(context.MergeInput);
        if (context.ViewModel.AutoResolveTask is not null)
            await context.ViewModel.AutoResolveTask;

        // AI auto-resolves on load, removing conflict markers
        context.ViewModel.HasAiResolved.Should().BeTrue();

        // Accept is NOT enabled until user approves all conflicts
        context.ViewModel.AcceptCommand.CanExecute(null).Should().BeFalse();
        context.ViewModel.MergedResultViewModel.AllConflictsApproved.Should().BeFalse();

        // Simulate user approving every conflict (clicking each ! in the gutter)
        foreach (var item in context.ViewModel.MergedResultViewModel.ApprovalItems)
        {
            if (item.State == ConflictApprovalState.Resolved)
            {
                item.State = ConflictApprovalState.Approved;
            }
        }

        context.ViewModel.MergedResultViewModel.AllConflictsApproved.Should().BeTrue();
        context.ViewModel.AcceptCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task Accept_command_writes_output_when_valid()
    {
        var context = CreateContext(withConflictMarkers: false);

        await context.ViewModel.InitializeAsync(context.MergeInput);
        context.ViewModel.MergedResultViewModel.Content = "resolved";

        await context.ViewModel.AcceptCommand.ExecuteAsync(null);

        var written = await File.ReadAllTextAsync(context.OutputPath);
        written.Should().Be("resolved");
    }

    [Fact]
    public async Task Cancel_command_sets_session_cancelled()
    {
        var context = CreateContext(withConflictMarkers: false);

        await context.ViewModel.InitializeAsync(context.MergeInput);
        context.ViewModel.CancelCommand.Execute(null);

        context.SessionManager.CurrentSession!.State.Should().Be(SessionState.Cancelled);
    }

    [Fact]
    public async Task Accept_error_sets_error_message()
    {
        var context = CreateContext(withConflictMarkers: false, throwOnWrite: true);

        await context.ViewModel.InitializeAsync(context.MergeInput);
        context.ViewModel.MergedResultViewModel.Content = "resolved";

        await context.ViewModel.AcceptCommand.ExecuteAsync(null);

        context.ViewModel.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    private static TestContext CreateContext(bool withConflictMarkers, bool throwOnWrite = false)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "AutoMerge.UI.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var baseContent = "line1\nline2\n";
        var localContent = "line1\nlocal\n";
        var remoteContent = "line1\nremote\n";
        var mergedContent = withConflictMarkers
            ? "line1\n<<<<<<< LOCAL\nlocal\n=======\nremote\n>>>>>>> REMOTE\n"
            : "line1\nresolved\n";

        var basePath = WriteFile(tempDir, "base.txt", baseContent);
        var localPath = WriteFile(tempDir, "local.txt", localContent);
        var remotePath = WriteFile(tempDir, "remote.txt", remoteContent);
        var outputPath = WriteFile(tempDir, "merged.txt", mergedContent);

        var fileService = new TestFileService(throwOnWrite);
        var conflictParser = new ConflictMarkerParser();
        var eventAggregator = new EventAggregator();
        var sessionManager = new MergeSessionManager(eventAggregator);

        var loadHandler = new LoadMergeSessionHandler(fileService, conflictParser, sessionManager, eventAggregator);
        var aiService = new MockAiService();
        var proposeHandler = new ProposeResolutionHandler(aiService, sessionManager, eventAggregator, new InMemoryConfigurationService());
        var acceptHandler = new AcceptResolutionHandler(fileService, conflictParser, sessionManager, new AutoSaveService(fileService), eventAggregator);
        var cancelHandler = new CancelMergeHandler(sessionManager, new AutoSaveService(fileService), eventAggregator);
        var refineHandler = new RefineResolutionHandler(aiService, sessionManager, eventAggregator);

        var diffCalculator = Substitute.For<IDiffCalculator>();
        diffCalculator.CalculateDiff(Arg.Any<string>(), Arg.Any<string>()).Returns(Array.Empty<LineChange>());
        diffCalculator.CalculateDiffForNewText(Arg.Any<string>(), Arg.Any<string>()).Returns(Array.Empty<LineChange>());

        var mergedResultViewModel = new MergedResultViewModel(conflictParser, diffCalculator);
        var aiChatViewModel = new AiChatViewModel(refineHandler, eventAggregator);

        var viewModel = new MainWindowViewModel(
            loadHandler,
            proposeHandler,
            acceptHandler,
            cancelHandler,
            fileService,
            diffCalculator,
            aiService,
            mergedResultViewModel,
            aiChatViewModel);

        return new TestContext(viewModel, sessionManager, basePath, localPath, remotePath, outputPath, baseContent, localContent, remoteContent, mergedContent);
    }

    private static string WriteFile(string directory, string fileName, string content)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private sealed record TestContext(
        MainWindowViewModel ViewModel,
        MergeSessionManager SessionManager,
        string BasePath,
        string LocalPath,
        string RemotePath,
        string OutputPath,
        string BaseContent,
        string LocalContent,
        string RemoteContent,
        string MergedContent)
    {
        public MergeInput MergeInput => new(BasePath, LocalPath, RemotePath, OutputPath);
    }

    private sealed class TestFileService : IFileService
    {
        private readonly bool _throwOnWrite;

        public TestFileService(bool throwOnWrite)
        {
            _throwOnWrite = throwOnWrite;
        }

        public async Task<FileContent> ReadAsync(string path, CancellationToken cancellationToken)
        {
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            var encoding = EncodingDetector.Detect(bytes);
            var content = encoding.GetString(bytes);
            var lineEnding = LineEndingDetector.Detect(content);
            return new FileContent(content, encoding, lineEnding);
        }

        public Task WriteAsync(string path, string content, Encoding encoding, LineEnding lineEnding, CancellationToken cancellationToken)
        {
            if (_throwOnWrite)
            {
                throw new IOException("Simulated write failure.");
            }

            return File.WriteAllTextAsync(path, content, encoding, cancellationToken);
        }

        public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(File.Exists(path));
        }

        public async Task<bool> IsBinaryAsync(string path, CancellationToken cancellationToken)
        {
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            return bytes.Any(b => b == 0);
        }
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
