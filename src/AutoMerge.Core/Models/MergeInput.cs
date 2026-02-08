using System;
using AutoMerge.Core.Localization;

namespace AutoMerge.Core.Models;

public sealed record MergeInput
{
    public string BasePath { get; }
    public string LocalPath { get; }
    public string RemotePath { get; }
    public string OutputPath { get; }

    public MergeInput(string basePath, string localPath, string remotePath, string outputPath)
    {
        BasePath = RequirePath(basePath, nameof(basePath));
        LocalPath = RequirePath(localPath, nameof(localPath));
        RemotePath = RequirePath(remotePath, nameof(remotePath));
        OutputPath = RequirePath(outputPath, nameof(outputPath));
    }

    private static string RequirePath(string path, string paramName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(CoreStrings.PathMustNotBeNullOrEmpty, paramName);
        }

        return path;
    }
}
