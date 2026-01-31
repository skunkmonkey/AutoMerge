using AutoMerge.Core.Models;

namespace AutoMerge.Application.UseCases.AnalyzeConflict;

public sealed record AnalyzeConflictResult(bool Success, ConflictAnalysis? Analysis, string? ErrorMessage);
