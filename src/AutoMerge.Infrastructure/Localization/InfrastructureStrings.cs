using System.Globalization;
using System.Resources;
using AutoMerge.Resources;

namespace AutoMerge.Infrastructure.Localization;

internal static class InfrastructureStrings
{
    private static readonly ResourceManager ResourceManager = new("AutoMerge.Resources.Resources.Infrastructure.Strings", typeof(ResourceAssembly).Assembly);

    public static string CopilotClientStartFailed => GetString(nameof(CopilotClientStartFailed));
    public static string CopilotCliNotFound => GetString(nameof(CopilotCliNotFound));
    public static string CopilotCliAuthRequired => GetString(nameof(CopilotCliAuthRequired));
    public static string CopilotConnectionFailedFormat => GetString(nameof(CopilotConnectionFailedFormat));
    public static string CopilotAnalysisFailed => GetString(nameof(CopilotAnalysisFailed));
    public static string AiProposedResolution => GetString(nameof(AiProposedResolution));
    public static string CopilotResolutionProposalFailed => GetString(nameof(CopilotResolutionProposalFailed));
    public static string RefinedMessageFormat => GetString(nameof(RefinedMessageFormat));
    public static string CopilotRefinementFailed => GetString(nameof(CopilotRefinementFailed));
    public static string CopilotExplanationFailed => GetString(nameof(CopilotExplanationFailed));
    public static string CopilotIntentResearchFailedFormat => GetString(nameof(CopilotIntentResearchFailedFormat));
    public static string ClientNotStarted => GetString(nameof(ClientNotStarted));
    public static string SessionError => GetString(nameof(SessionError));
    public static string AiRequestTimeoutFormat => GetString(nameof(AiRequestTimeoutFormat));
    public static string MockLocalChangeSummary => GetString(nameof(MockLocalChangeSummary));
    public static string MockRemoteChangeSummary => GetString(nameof(MockRemoteChangeSummary));
    public static string MockConflictReason => GetString(nameof(MockConflictReason));
    public static string MockSuggestedApproach => GetString(nameof(MockSuggestedApproach));
    public static string MockResolvedContent => GetString(nameof(MockResolvedContent));
    public static string MockResolutionExplanation => GetString(nameof(MockResolutionExplanation));
    public static string MockExplanation => GetString(nameof(MockExplanation));
    public static string MockModelName => GetString(nameof(MockModelName));
    public static string MockLocalIntent => GetString(nameof(MockLocalIntent));
    public static string MockRemoteIntent => GetString(nameof(MockRemoteIntent));
    public static string MergeAgentSystemPrompt => GetString(nameof(MergeAgentSystemPrompt));
    public static string AnalysisPromptTemplate => GetString(nameof(AnalysisPromptTemplate));
    public static string ResolutionPromptTemplate => GetString(nameof(ResolutionPromptTemplate));
    public static string RefinementPromptTemplate => GetString(nameof(RefinementPromptTemplate));
    public static string IntentResearchPromptTemplate => GetString(nameof(IntentResearchPromptTemplate));
    public static string IntentAwareResolutionPromptTemplate => GetString(nameof(IntentAwareResolutionPromptTemplate));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
    }
}
