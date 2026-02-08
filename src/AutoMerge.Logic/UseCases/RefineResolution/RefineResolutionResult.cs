using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.RefineResolution;

public sealed record RefineResolutionResult(bool Success, MergeResolution? Resolution, string? ErrorMessage);
