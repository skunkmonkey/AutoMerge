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
}
