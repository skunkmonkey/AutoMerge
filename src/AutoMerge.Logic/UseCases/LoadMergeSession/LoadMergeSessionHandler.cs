using System.Globalization;
using AutoMerge.Logic.Events;
using AutoMerge.Logic.Localization;
using AutoMerge.Logic.Services;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.LoadMergeSession;

public sealed class LoadMergeSessionHandler
{
    private readonly IFileService _fileService;
    private readonly IConflictParser _conflictParser;
    private readonly MergeSessionManager _sessionManager;
    private readonly IEventAggregator _eventAggregator;

    public LoadMergeSessionHandler(
        IFileService fileService,
        IConflictParser conflictParser,
        MergeSessionManager sessionManager,
        IEventAggregator eventAggregator)
    {
        _fileService = fileService;
        _conflictParser = conflictParser;
        _sessionManager = sessionManager;
        _eventAggregator = eventAggregator;
    }

    public async Task<LoadMergeSessionResult> ExecuteAsync(
        LoadMergeSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var input = command.MergeInput ?? throw new ArgumentException(LogicStrings.MergeInputRequired, nameof(command));

        var requiredPaths = new[] { input.BasePath, input.LocalPath, input.RemotePath };
        foreach (var path in requiredPaths)
        {
            if (!await _fileService.ExistsAsync(path, cancellationToken).ConfigureAwait(false))
            {
                return new LoadMergeSessionResult(false, string.Format(CultureInfo.CurrentCulture, LogicStrings.MissingRequiredFileFormat, path), null);
            }

            if (await _fileService.IsBinaryAsync(path, cancellationToken).ConfigureAwait(false))
            {
                return new LoadMergeSessionResult(false, string.Format(CultureInfo.CurrentCulture, LogicStrings.BinaryFileNotSupportedFormat, path), null);
            }
        }

        var mergedExists = await _fileService.ExistsAsync(input.OutputPath, cancellationToken).ConfigureAwait(false);
        if (mergedExists && await _fileService.IsBinaryAsync(input.OutputPath, cancellationToken).ConfigureAwait(false))
        {
            return new LoadMergeSessionResult(false, string.Format(CultureInfo.CurrentCulture, LogicStrings.BinaryFileNotSupportedFormat, input.OutputPath), null);
        }

        try
        {
            FileContent conflictContent;
            string conflictFilePath;
            if (mergedExists)
            {
                conflictFilePath = input.OutputPath;
                conflictContent = await _fileService.ReadAsync(input.OutputPath, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                conflictFilePath = input.LocalPath;
                conflictContent = await _fileService.ReadAsync(input.LocalPath, cancellationToken).ConfigureAwait(false);
            }

            var conflictRegions = _conflictParser.Parse(conflictContent.Content);
            var conflictFile = new ConflictFile(
                conflictFilePath,
                conflictContent.Content,
                conflictContent.Encoding,
                conflictContent.DetectedLineEnding,
                conflictRegions);

            var session = _sessionManager.CreateSession(input);
            session.SetConflictFile(conflictFile);
            session.SetMergedContent(conflictContent.Content);
            session.SetState(SessionState.Ready);

            _eventAggregator.Publish(new SessionLoadedEvent());

            return new LoadMergeSessionResult(true, null, session);
        }
        catch (Exception ex)
        {
            return new LoadMergeSessionResult(false, ex.Message, null);
        }
    }
}
