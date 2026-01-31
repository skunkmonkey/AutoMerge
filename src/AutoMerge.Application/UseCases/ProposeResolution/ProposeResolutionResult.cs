using AutoMerge.Core.Models;

namespace AutoMerge.Application.UseCases.ProposeResolution;

public sealed record ProposeResolutionResult(bool Success, MergeResolution? Resolution, string? ErrorMessage);
