using System.Text;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.Core.Services;

namespace AutoMerge.Infrastructure.FileSystem;

public sealed class FileService : IFileService
{
    private const int BinaryProbeLength = 8 * 1024;

    public async Task<FileContent> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var encoding = EncodingDetector.Detect(bytes);
        var content = encoding.GetString(bytes);
        var lineEnding = LineEndingDetector.Detect(content);
        return new FileContent(content, encoding, lineEnding);
    }

    public async Task WriteAsync(
        string path,
        string content,
        Encoding encoding,
        LineEnding lineEnding,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeLineEndings(content, lineEnding);
        var bytes = encoding.GetBytes(normalized);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken)
    {
        return Task.FromResult(File.Exists(path));
    }

    public async Task<bool> IsBinaryAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var buffer = new byte[BinaryProbeLength];
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < read; i++)
        {
            if (buffer[i] == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeLineEndings(string content, LineEnding lineEnding)
    {
        if (lineEnding == LineEnding.Mixed)
        {
            return content;
        }

        var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
        return lineEnding == LineEnding.CRLF
            ? normalized.Replace("\n", "\r\n")
            : normalized;
    }
}
