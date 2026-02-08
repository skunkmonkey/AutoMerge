using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.LoadMergeSession;

public sealed record LoadMergeSessionResult(bool Success, string? ErrorMessage, MergeSession? Session);
