using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMerge.Core.Models;

namespace AutoMerge.Core.Abstractions;

public interface IFileService
{
    Task<FileContent> ReadAsync(string path, CancellationToken cancellationToken);

    Task WriteAsync(
        string path,
        string content,
        Encoding encoding,
        LineEnding lineEnding,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken);

    Task<bool> IsBinaryAsync(string path, CancellationToken cancellationToken);
}
