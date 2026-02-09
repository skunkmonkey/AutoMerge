using System.Text;

namespace AutoMerge.Core.Models;

public sealed record FileContent(string Content, Encoding Encoding, LineEnding DetectedLineEnding);
