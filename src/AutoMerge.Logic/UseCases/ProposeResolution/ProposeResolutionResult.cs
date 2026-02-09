using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.ProposeResolution;

public sealed record ProposeResolutionResult(
    bool Success,
    MergeResolution? Resolution,
    string? ErrorMessage,
    string? LocalIntent = null,
    string? RemoteIntent = null);
