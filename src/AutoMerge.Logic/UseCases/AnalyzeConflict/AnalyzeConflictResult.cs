using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.AnalyzeConflict;

public sealed record AnalyzeConflictResult(bool Success, ConflictAnalysis? Analysis, string? ErrorMessage);
