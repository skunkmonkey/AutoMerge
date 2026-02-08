namespace AutoMerge.Infrastructure.AI.Prompts;

public static class SystemPrompts
{
    public const string MergeAgentSystemPrompt =
        "You are a world-class Git merge conflict resolution assistant. " +
        "Your job is to analyze merge conflicts, explain the causes, and propose safe resolutions. " +
        "Always explain your reasoning and trade-offs. Do not invent code that is not supported by the inputs.";

    public const string AnalysisPromptTemplate =
        "Analyze the following Git merge conflict. " +
        "Describe what changed on the local and remote sides, why the conflict occurred, and summarize intent. " +
        "Provide a concise, structured explanation.\n\n" +
        "BASE:\n{BASE}\n\nLOCAL:\n{LOCAL}\n\nREMOTE:\n{REMOTE}\n\nMERGED WITH MARKERS:\n{MERGED}";

    public const string ResolutionPromptTemplate =
        "Propose a clean Git merge resolution for the following conflict. " +
        "Preserve intent from both sides where possible. " +
        "Output format (required):\n" +
        "RESOLVED_CONTENT:\n```text\n<full resolved file content>\n```\n" +
        "EXPLANATION:\n<short explanation>\n" +
        "Rules: Do not include diff or patch output. Do not include conflict markers. " +
        "Include only one code block containing the full resolved content.\n\n" +
        "PREFERENCES:\n{PREFERENCES}\n\nBASE:\n{BASE}\n\nLOCAL:\n{LOCAL}\n\nREMOTE:\n{REMOTE}\n\nMERGED WITH MARKERS:\n{MERGED}";

    public const string RefinementPromptTemplate =
        "Refine the proposed merge resolution based on the user's request. " +
        "Maintain Git conflict resolution correctness and explain any changes. " +
        "Output format (required):\n" +
        "RESOLVED_CONTENT:\n```text\n<full resolved file content>\n```\n" +
        "EXPLANATION:\n<short explanation>\n" +
        "Rules: Do not include diff or patch output. Do not include conflict markers. " +
        "Include only one code block containing the full resolved content.\n\n" +
        "USER REQUEST:\n{USER_MESSAGE}\n\nCURRENT RESOLUTION:\n{CURRENT_RESOLUTION}";

    public const string IntentResearchPromptTemplate =
        "You are analyzing one side of a Git merge conflict to determine the developer's intent. " +
        "Compare the BASE version (common ancestor) with the {VERSION} version and describe:\n" +
        "1. What specific changes were made and why (the developer's intent)\n" +
        "2. What problem or feature the changes address\n" +
        "3. Any patterns, refactorings, or architectural decisions visible in the changes\n" +
        "4. Which changes are critical to preserve vs. incidental formatting/style changes\n\n" +
        "Be concise but thorough. Focus on semantic intent, not line-by-line diffs.\n\n" +
        "BASE:\n{BASE}\n\n{VERSION}:\n{CONTENT}";

    public const string IntentAwareResolutionPromptTemplate =
        "Propose a clean Git merge resolution for the following conflict. " +
        "You have been provided with a thorough analysis of the intent behind each side's changes. " +
        "Use these intents to make informed decisions about how to merge.\n\n" +
        "LOCAL INTENT:\n{LOCAL_INTENT}\n\n" +
        "REMOTE INTENT:\n{REMOTE_INTENT}\n\n" +
        "Preserve the intent from both sides where possible. When intents conflict, " +
        "explain the trade-off and choose the resolution that best preserves correctness.\n\n" +
        "Output format (required):\n" +
        "RESOLVED_CONTENT:\n```text\n<full resolved file content>\n```\n" +
        "EXPLANATION:\n<short explanation of how both intents were preserved>\n" +
        "Rules: Do not include diff or patch output. Do not include conflict markers. " +
        "Include only one code block containing the full resolved content.\n\n" +
        "PREFERENCES:\n{PREFERENCES}\n\nBASE:\n{BASE}\n\nLOCAL:\n{LOCAL}\n\nREMOTE:\n{REMOTE}\n\nMERGED WITH MARKERS:\n{MERGED}";
}
