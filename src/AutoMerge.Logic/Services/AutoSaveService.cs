using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Logic.Services;

public sealed class AutoSaveService : IDisposable
{
    private static readonly TimeSpan AutoSaveInterval = TimeSpan.FromSeconds(30);

    private readonly IFileService _fileService;
    private readonly object _sync = new();
    private Timer? _timer;
    private MergeSession? _session;

    public AutoSaveService(IFileService fileService)
    {
        _fileService = fileService;
    }

    public void StartAutoSave(MergeSession session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        lock (_sync)
        {
            _session = session;
            _timer?.Dispose();
            _timer = new Timer(_ => _ = SaveDraftNow(), null, AutoSaveInterval, AutoSaveInterval);
        }
    }

    public void StopAutoSave()
    {
        lock (_sync)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    public async Task SaveDraftNow(CancellationToken cancellationToken = default)
    {
        MergeSession? session;
        lock (_sync)
        {
            session = _session;
        }

        if (session is null)
        {
            return;
        }

        var content = session.CurrentMergedContent;
        var encoding = session.ConflictFile?.Encoding ?? new UTF8Encoding(false);
        var lineEnding = session.ConflictFile?.LineEnding ?? LineEnding.LF;
        var path = GetDraftPath(session.Id);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await _fileService.WriteAsync(path, content, encoding, lineEnding, cancellationToken).ConfigureAwait(false);
    }

    public void CleanupDrafts()
    {
        MergeSession? session;
        lock (_sync)
        {
            session = _session;
        }

        if (session is null)
        {
            return;
        }

        var path = GetDraftPath(session.Id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void Dispose()
    {
        StopAutoSave();
    }

    private static string GetDraftPath(Guid sessionId)
    {
        var directory = Path.Combine(Path.GetTempPath(), "AutoMerge");
        return Path.Combine(directory, $"draft-{sessionId}.txt");
    }
}
