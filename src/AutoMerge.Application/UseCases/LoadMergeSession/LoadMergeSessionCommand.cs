using AutoMerge.Core.Models;

namespace AutoMerge.Application.UseCases.LoadMergeSession;

public sealed record LoadMergeSessionCommand(MergeInput MergeInput);
