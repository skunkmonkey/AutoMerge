# AutoMerge Product Requirements Document (PRD)

**Version:** 2.0  
**Date:** January 31, 2026  
**Status:** Draft  
**Stakeholders:** Developers, DevOps/Release Engineers, Product Manager, IT/Platform Team  

---

## 1. Executive Summary

AutoMerge is a cross-platform, AI-powered merge conflict resolution tool designed to integrate seamlessly with Git workflows. Built on the **GitHub Copilot SDK**, AutoMerge leverages production-grade AI agent orchestration to analyze conflicts, understand code semantics, and propose intelligent resolutions—all while keeping developers in full control.

The tool operates as a standalone desktop application with full command-line interface support, enabling integration with popular Git clients like SourceTree, GitKraken, Fork, and Tower. By using the Copilot SDK's agent runtime, AutoMerge inherits battle-tested AI capabilities without the complexity of building custom model orchestration.

---

## 2. Problem Statement

Merge conflicts remain one of the most frustrating and time-consuming aspects of collaborative software development:

- **Manual Interpretation:** Developers must mentally parse 3-way diffs, understand the intent of each change, and reconcile differences—often across unfamiliar code.
- **Error-Prone:** Manual resolution increases the risk of subtle bugs, lost changes, or broken functionality that may not surface until production.
- **Context Switching:** Resolving conflicts interrupts flow, requiring developers to stop feature work and context-switch to understanding merge semantics.
- **Tool Fragmentation:** Existing merge tools lack AI assistance, while AI assistants lack merge tool integration.

AutoMerge solves this by providing an intelligent merge resolution experience that:
- Understands code semantics, not just text differences
- Explains *why* conflicts occurred and *how* proposed resolutions preserve intent
- Integrates directly into existing Git client workflows via standard merge tool protocols
- Uses GitHub Copilot's proven AI infrastructure for reliable, high-quality suggestions

---

## 3. Goals and Objectives

### Primary Goals
1. **Reduce Resolution Time:** Cut average merge conflict resolution time by 50%+ through AI-assisted analysis and suggestions.
2. **Improve Resolution Quality:** Decrease post-merge bugs and reverts by providing semantic-aware resolutions.
3. **Seamless Integration:** Work as a drop-in replacement for existing merge tools in Git clients and CLI workflows.
4. **Cross-Platform:** First-class support for both Windows and macOS.

### Secondary Goals
- Leverage GitHub Copilot subscription users already have (no additional AI costs)
- Provide educational value by explaining conflict causes and resolution reasoning
- Support team consistency through shareable resolution preferences and patterns

---

## 4. Target Users

### Primary Persona: Professional Developer
- Uses Git daily with frequent branching/merging
- Has a GitHub Copilot subscription (individual or enterprise)
- Uses SourceTree, Fork, GitKraken, Tower, or command-line Git
- Values efficiency and code quality over manual control for routine merges

### Secondary Persona: Team Lead / DevOps Engineer
- Manages repositories with multiple contributors
- Needs to resolve conflicts quickly during release processes
- Wants consistent merge resolution patterns across the team

---

## 5. User Stories

### US-001: Launch from Git Client
**As a** developer using SourceTree  
**I want to** configure AutoMerge as my merge tool  
**So that** it launches automatically when I encounter conflicts  

**Acceptance Criteria:**
- AutoMerge accepts standard merge tool command-line arguments (`$BASE`, `$LOCAL`, `$REMOTE`, `$MERGED`)
- Supports both 3-way and 4-way merge tool protocols
- Returns appropriate exit codes (0 = resolved, 1 = cancelled/failed)
- Works with SourceTree, Fork, GitKraken, Tower, and command-line `git mergetool`

### US-002: View and Understand Conflicts
**As a** developer  
**I want to** see a clear visualization of the conflict with AI-generated explanations  
**So that** I understand what changed and why it conflicts  

**Acceptance Criteria:**
- Display 3-way diff view (base, ours, theirs) with syntax highlighting
- AI-generated summary explaining what each side changed
- Highlight specific lines/regions causing the conflict
- Support common file types (code, config, markdown, JSON, YAML)

### US-003: Get AI-Suggested Resolution
**As a** developer  
**I want to** receive an AI-suggested resolution with reasoning  
**So that** I can quickly accept good suggestions or understand the context for manual edits  

**Acceptance Criteria:**
- AI analyzes conflict using Copilot SDK agent
- Provides a proposed merged result with explanation
- Explains trade-offs if both changes can't be fully preserved
- Streaming display shows AI "thinking" progress
- Resolution appears in editable fourth pane

### US-004: Interactive Resolution Refinement
**As a** developer  
**I want to** ask the AI follow-up questions or request adjustments  
**So that** I can refine the resolution without starting over  

**Acceptance Criteria:**
- Chat interface for natural language interaction with the AI agent
- Can request: "Keep the function signature from ours but the implementation from theirs"
- Can ask: "Why did you choose this approach?"
- AI maintains context from the current conflict session
- Conversation history visible in sidebar

### US-005: Manual Override and Editing
**As a** developer  
**I want to** directly edit the proposed resolution  
**So that** I maintain full control over the final result  

**Acceptance Criteria:**
- Resolution pane is fully editable with syntax highlighting
- Real-time validation shows if result is Git-compliant
- Can revert to any of the three input versions with one click
- Undo/redo support for all edits

### US-006: Accept and Save Resolution
**As a** developer  
**I want to** accept the resolution and return to my Git client  
**So that** I can continue my merge workflow  

**Acceptance Criteria:**
- "Accept" writes resolved content to the output file
- Application exits with code 0 signaling success to Git
- "Cancel" exits with code 1, leaving conflict unresolved
- Keyboard shortcuts: Cmd/Ctrl+Enter (accept), Escape (cancel)

### US-007: Batch Conflict Resolution
**As a** developer with multiple conflicts  
**I want to** navigate between conflicts in the same merge session  
**So that** I can resolve all conflicts efficiently  

**Acceptance Criteria:**
- When launched for multiple files, show conflict navigator
- Previous/Next buttons to move between conflicts
- Progress indicator showing resolved/remaining counts
- "Accept All AI Suggestions" option for confident batch resolution

### US-008: Copilot Authentication
**As a** developer with a GitHub Copilot subscription  
**I want to** authenticate once and have it persist  
**So that** I don't need to re-authenticate each session  

**Acceptance Criteria:**
- On first launch, prompt for GitHub authentication via Copilot CLI
- Credential caching via Copilot CLI's standard mechanism
- Clear indicator of authentication status
- Graceful handling of expired/invalid tokens with re-auth flow

### US-009: Configure Resolution Preferences
**As a** developer  
**I want to** configure my resolution preferences  
**So that** the AI suggestions match my team's patterns  

**Acceptance Criteria:**
- Preference: Default bias (ours/theirs/balanced)
- Preference: Formatting style (preserve original/normalize)
- Preference: Comment style for complex resolutions
- Preference: Model selection (when multiple available)
- Preferences persist across sessions

---

## 6. Functional Requirements

### 6.1 Command-Line Interface

| Requirement | Description |
|-------------|-------------|
| FR-CLI-001 | Accept 4-file merge tool arguments: `--base <path> --local <path> --remote <path> --merged <path>` |
| FR-CLI-002 | Accept 3-file merge tool arguments: `--local <path> --remote <path> --merged <path>` |
| FR-CLI-003 | Support positional arguments for compatibility: `automerge BASE LOCAL REMOTE MERGED` |
| FR-CLI-004 | Support `--help` and `--version` flags |
| FR-CLI-005 | Return exit code 0 on successful resolution, 1 on cancel/failure |
| FR-CLI-006 | Support `--no-gui` flag for future headless/CI mode |
| FR-CLI-007 | Support `--wait` flag for synchronous operation (block until closed) |

### 6.2 User Interface

| Requirement | Description |
|-------------|-------------|
| FR-UI-001 | Display 4-pane layout: Base, Local (Ours), Remote (Theirs), Merged Result |
| FR-UI-002 | Provide syntax highlighting for 50+ common file types |
| FR-UI-003 | Show line-level diff highlighting (additions green, deletions red, changes yellow) |
| FR-UI-004 | Provide collapsible AI chat panel for interactive refinement |
| FR-UI-005 | Show AI "thinking" indicator with streaming progress |
| FR-UI-006 | Provide conflict navigation for multi-file merges |
| FR-UI-007 | Support dark and light themes matching system preference |
| FR-UI-008 | Provide keyboard shortcuts for all major actions |

### 6.3 AI Integration (Copilot SDK)

| Requirement | Description |
|-------------|-------------|
| FR-AI-001 | Initialize Copilot SDK client with automatic CLI lifecycle management |
| FR-AI-002 | Create dedicated session per merge operation with merge-specialized system prompt |
| FR-AI-003 | Define custom tools: `analyze_conflict`, `propose_resolution`, `explain_changes`, `validate_result` |
| FR-AI-004 | Attach conflict files to session context for AI analysis |
| FR-AI-005 | Stream AI responses to UI in real-time |
| FR-AI-006 | Support conversational refinement within the session |
| FR-AI-007 | Implement `onPreToolUse` hook to confirm before any file writes |
| FR-AI-008 | Handle Copilot API rate limits and errors gracefully |

### 6.4 Merge Resolution

| Requirement | Description |
|-------------|-------------|
| FR-MRG-001 | Parse Git conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`, `|||||||`) |
| FR-MRG-002 | Support diff3-style conflicts (with base section) |
| FR-MRG-003 | Validate resolved output has no remaining conflict markers |
| FR-MRG-004 | Write resolved content with correct line endings (detect from input) |
| FR-MRG-005 | Preserve file encoding (UTF-8, UTF-16, etc.) |
| FR-MRG-006 | Support binary file detection with appropriate messaging |

### 6.5 Configuration and Persistence

| Requirement | Description |
|-------------|-------------|
| FR-CFG-001 | Store preferences in platform-appropriate location (`%APPDATA%` / `~/Library/Application Support`) |
| FR-CFG-002 | Never persist file paths or repository information |
| FR-CFG-003 | Provide UI for viewing and editing preferences |
| FR-CFG-004 | Support preference reset to defaults |
| FR-CFG-005 | Cache authentication via Copilot CLI (no custom token storage) |

---

## 7. Non-Functional Requirements

### 7.1 Performance

| Requirement | Description |
|-------------|-------------|
| NFR-PERF-001 | Application cold start < 2 seconds |
| NFR-PERF-002 | File loading and initial render < 500ms for files up to 10,000 lines |
| NFR-PERF-003 | AI response streaming begins < 3 seconds after request |
| NFR-PERF-004 | UI interactions respond within 100ms |

### 7.2 Reliability

| Requirement | Description |
|-------------|-------------|
| NFR-REL-001 | Graceful degradation if Copilot service unavailable (manual-only mode) |
| NFR-REL-002 | No data loss on crash (auto-save drafts every 30 seconds) |
| NFR-REL-003 | Timeout AI requests after 60 seconds with retry option |

### 7.3 Security and Privacy

| Requirement | Description |
|-------------|-------------|
| NFR-SEC-001 | File contents sent only to GitHub Copilot (same privacy model as Copilot in IDE) |
| NFR-SEC-002 | No telemetry or analytics without explicit opt-in |
| NFR-SEC-003 | No local storage of file contents or diffs |
| NFR-SEC-004 | Authentication via GitHub OAuth (managed by Copilot CLI) |

### 7.4 Compatibility

| Requirement | Description |
|-------------|-------------|
| NFR-COMPAT-001 | Windows 10/11 (x64 and ARM64) |
| NFR-COMPAT-002 | macOS 12+ (Intel and Apple Silicon) |
| NFR-COMPAT-003 | Verified integration: SourceTree, Fork, GitKraken, Tower, VS Code Git |
| NFR-COMPAT-004 | Standard `git mergetool` compatibility |

### 7.5 Accessibility

| Requirement | Description |
|-------------|-------------|
| NFR-A11Y-001 | Full keyboard navigation |
| NFR-A11Y-002 | Screen reader compatibility for major UI elements |
| NFR-A11Y-003 | Minimum 4.5:1 contrast ratio |
| NFR-A11Y-004 | Respect system font size preferences |

---

## 8. Technical Architecture

### 8.1 Technology Stack

| Component | Technology | Rationale |
|-----------|------------|-----------|
| **Runtime** | .NET 8 | Cross-platform, native performance, excellent Copilot SDK support |
| **UI Framework** | Avalonia UI | True cross-platform native UI (not Electron), XAML-based |
| **AI Integration** | GitHub Copilot SDK (.NET) | Official SDK, handles agent orchestration and CLI lifecycle |
| **Diff Engine** | DiffPlex | Mature .NET diff library, supports various diff algorithms |
| **Text Editor** | AvaloniaEdit | Feature-rich code editor with syntax highlighting |

### 8.2 Copilot SDK Integration Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      AutoMerge UI                           │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────────────┐   │
│  │  Base   │ │  Ours   │ │ Theirs  │ │  Merged Result  │   │
│  │  Pane   │ │  Pane   │ │  Pane   │ │     (Edit)      │   │
│  └─────────┘ └─────────┘ └─────────┘ └─────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              AI Chat / Refinement Panel              │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    AutoMerge Core                           │
│  ┌─────────────────┐  ┌─────────────────────────────────┐  │
│  │  Conflict Parser │  │      Copilot Session Manager    │  │
│  │  (Git markers)   │  │  - Session lifecycle            │  │
│  └─────────────────┘  │  - Custom tools registration     │  │
│  ┌─────────────────┐  │  - Streaming response handling   │  │
│  │  File Manager   │  │  - Conversation state            │  │
│  │  (I/O, encoding)│  └─────────────────────────────────┘  │
│  └─────────────────┘                                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼ JSON-RPC
┌─────────────────────────────────────────────────────────────┐
│                    GitHub Copilot CLI                       │
│              (Managed by Copilot SDK Client)                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Agent Runtime │ Tool Execution │ Model Routing     │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼ HTTPS
┌─────────────────────────────────────────────────────────────┐
│                  GitHub Copilot Service                     │
│          (Authentication, Model Access, Billing)            │
└─────────────────────────────────────────────────────────────┘
```

### 8.3 Custom Copilot Tools

The following tools will be registered with the Copilot session:

**`analyze_conflict`**
- Input: Base, local, and remote file contents
- Output: Structured analysis (conflict regions, change descriptions, semantic impact)

**`propose_resolution`**
- Input: Conflict analysis, optional user preferences
- Output: Merged content with inline explanations

**`explain_changes`**
- Input: Specific line range or region
- Output: Natural language explanation of what changed and why

**`validate_result`**
- Input: Proposed merged content
- Output: Validation result (no conflict markers, syntax valid if parseable)

---

## 9. Git Client Integration

### 9.1 SourceTree Configuration
```
Merge Tool: Custom
Command: /path/to/automerge
Arguments: --base "$BASE" --local "$LOCAL" --remote "$REMOTE" --merged "$MERGED"
```

### 9.2 Git Global Configuration
```bash
git config --global merge.tool automerge
git config --global mergetool.automerge.cmd 'automerge --base "$BASE" --local "$LOCAL" --remote "$REMOTE" --merged "$MERGED"'
git config --global mergetool.automerge.trustExitCode true
```

### 9.3 Tower / Fork / GitKraken
Similar custom tool configuration with the same argument pattern.

---

## 10. Invariants

1. **User Control:** The user must always be able to review and edit AI output before acceptance.
2. **No Silent Writes:** The system must not write to any file unless the user explicitly accepts the resolution.
3. **Exit Code Contract:** Exit code 0 means resolution was accepted and written; exit code 1 means no changes were made.
4. **Privacy Boundary:** File contents are only transmitted to GitHub Copilot service—no other external services.

---

## 11. Out of Scope

- Automatic merging without user review (CI/CD headless mode is future work)
- Full Git client functionality (fetch, commit, push, branch management)
- Diff viewing outside of merge conflicts
- Code refactoring or formatting beyond conflict resolution
- Support for non-Git version control systems (SVN, Mercurial, Perforce)
- Linux support (future release)

---

## 12. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Resolution Time | 50% reduction vs. manual | A/B timing study with pilot users |
| AI Suggestion Acceptance | >70% accepted without major edits | In-app telemetry (opt-in) |
| Post-Merge Reverts | <1% of AI-assisted resolutions | Git history analysis in pilot |
| User Satisfaction | >4.5/5 rating | Post-pilot survey |
| Adoption | 500+ downloads in first 90 days | Download tracking |

---

## 13. Risks and Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Copilot SDK breaking changes (tech preview) | High | Medium | Pin SDK version, maintain abstraction layer |
| AI suggests incorrect resolution | Medium | Medium | Always require human review, show confidence indicators |
| Copilot service outage | Medium | Low | Graceful degradation to manual-only mode |
| Complex conflicts exceed context window | Medium | Medium | Implement conflict chunking, focus on region |
| Users expect full automation | Low | Medium | Clear messaging that review is required |

---

## 14. Future Considerations

- **Headless/CI Mode:** `--no-gui --auto-accept-high-confidence` for automated pipelines
- **Team Patterns:** Shared resolution rules and preferences
- **MCP Integration:** Connect to GitHub MCP server for PR/issue context
- **Linux Support:** Extend Avalonia UI build to Linux
- **VS Code Extension:** Lightweight extension that launches AutoMerge for conflicts
- **Resolution History:** Learn from past resolutions in the repository

---

## 15. Open Questions

1. **Q:** Should we support BYOK (bring your own key) for users without Copilot subscriptions?  
   **Consideration:** Copilot SDK supports this, but adds complexity and support burden.

2. **Q:** Should the AI auto-run on launch, or wait for user to click "Analyze"?  
   **Consideration:** Auto-run is faster but may surprise users; could be a preference.

3. **Q:** How should we handle very large files (>50K lines)?  
   **Consideration:** May need chunked analysis or region-focused mode.

4. **Q:** Should we show token/cost estimates for Copilot usage?  
   **Consideration:** Copilot SDK usage counts toward premium request quota.

---

## 16. Appendix: Command-Line Reference

```
USAGE:
    automerge [OPTIONS] [BASE LOCAL REMOTE MERGED]

ARGUMENTS:
    BASE      Path to the base (common ancestor) file
    LOCAL     Path to the local (ours) file  
    REMOTE    Path to the remote (theirs) file
    MERGED    Path to write the merged output

OPTIONS:
    --base <PATH>       Path to base file (alternative to positional)
    --local <PATH>      Path to local file (alternative to positional)
    --remote <PATH>     Path to remote file (alternative to positional)
    --merged <PATH>     Path to merged output file (alternative to positional)
    --wait              Wait for GUI to close before returning (default: true)
    --no-gui            Run in headless mode (future feature)
    --help, -h          Show this help message
    --version, -v       Show version information

EXIT CODES:
    0    Resolution accepted and saved
    1    Resolution cancelled or error occurred

EXAMPLES:
    # Standard 4-file merge (for git mergetool)
    automerge BASE LOCAL REMOTE MERGED
    
    # Named arguments
    automerge --base=file.base --local=file.ours --remote=file.theirs --merged=file.out
    
    # SourceTree integration
    automerge "$BASE" "$LOCAL" "$REMOTE" "$MERGED"
```  
