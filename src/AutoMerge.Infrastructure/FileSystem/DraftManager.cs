using System.Text;

namespace AutoMerge.Infrastructure.FileSystem;

public sealed class DraftManager
{
    public string GetDraftPath(Guid sessionId)
    {
        var directory = Path.Combine(Path.GetTempPath(), "AutoMerge");
        return Path.Combine(directory, $"draft-{sessionId}.txt");
    }

    public async Task SaveDraftAsync(Guid sessionId, string content, Encoding encoding, CancellationToken cancellationToken = default)
    {
        var path = GetDraftPath(sessionId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var bytes = encoding.GetBytes(content);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> LoadDraftAsync(Guid sessionId, Encoding encoding, CancellationToken cancellationToken = default)
    {
        var path = GetDraftPath(sessionId);
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return encoding.GetString(bytes);
    }

    public void DeleteDraft(Guid sessionId)
    {
        var path = GetDraftPath(sessionId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
