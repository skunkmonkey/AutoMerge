using AutoMerge.Infrastructure.Localization;

namespace AutoMerge.Infrastructure.AI.Prompts;

public static class SystemPrompts
{
    public static string MergeAgentSystemPrompt => InfrastructureStrings.MergeAgentSystemPrompt;
    public static string AnalysisPromptTemplate => InfrastructureStrings.AnalysisPromptTemplate;
    public static string ResolutionPromptTemplate => InfrastructureStrings.ResolutionPromptTemplate;
    public static string RefinementPromptTemplate => InfrastructureStrings.RefinementPromptTemplate;
    public static string IntentResearchPromptTemplate => InfrastructureStrings.IntentResearchPromptTemplate;
    public static string IntentAwareResolutionPromptTemplate => InfrastructureStrings.IntentAwareResolutionPromptTemplate;
}
