using AutoMerge.Core.Models;

namespace AutoMerge.Application.UseCases.LoadMergeSession;

public sealed record LoadMergeSessionResult(bool Success, string? ErrorMessage, MergeSession? Session);
