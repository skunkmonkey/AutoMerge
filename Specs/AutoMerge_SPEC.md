# AutoMerge Technical Specification

**Version:** 1.5  
**Date:** February 7, 2026  
**Status:** Draft  
**Related Documents:** [AutoMerge_PRD.md](AutoMerge_PRD.md)

---

## 1. Purpose of This Document

This specification defines the software architecture for AutoMerge. Its purpose is to ensure that any developer (human or AI) working on this codebase understands:

1. **The architectural patterns and principles** that govern the codebase
2. **The responsibility of each layer and component**
3. **How components communicate** with each other
4. **Where specific functionality belongs** when adding new features
5. **The contracts and interfaces** that must be respected

This document is the **source of truth** for architectural decisions. All code contributions must adhere to this specification.

---

## 2. Architectural Principles

The following principles guide all architectural decisions:

### 2.1 Separation of Concerns
Each component has a single, well-defined responsibility. UI components do not contain business logic. Business logic does not contain infrastructure concerns. This separation enables independent testing and modification of each layer.

### 2.2 Dependency Inversion
High-level modules do not depend on low-level modules. Both depend on abstractions (interfaces). This allows swapping implementations (e.g., mock AI service for testing) without changing consuming code.

### 2.3 Explicit Dependencies
All dependencies are injected via constructor injection. No service locator patterns. No static access to services. This makes dependencies visible and testable.

### 2.4 Immutable Data Transfer
Data crossing layer boundaries uses immutable record types or read-only interfaces. This prevents unexpected mutations and makes data flow predictable.

### 2.5 Async by Default
All I/O operations (file access, AI calls, network) are asynchronous. The UI thread is never blocked by external operations.

### 2.6 Fail-Safe Behavior
The application must remain usable even when external services fail. AI unavailability degrades to manual-only mode. File access errors are handled gracefully with user feedback.

---

## 3. Solution Structure

The solution is organized into five source projects and five test projects:

**Source Projects:**

| Project | Purpose |
|---------|--------|
| AutoMerge.Core | Domain layer with zero external dependencies. Contains domain models, interfaces, and pure business logic. |
| AutoMerge.Logic | Application services and use case handlers. Orchestrates domain objects and infrastructure. |
| AutoMerge.Infrastructure | External integrations including Copilot SDK, file I/O, and configuration persistence. |
| AutoMerge.UI | Avalonia views (XAML) and ViewModels. All presentation logic. |
| AutoMerge.App | Composition root and entry point. CLI parsing, DI configuration, application bootstrap. |

**Test Projects:**

| Project | Purpose |
|---------|--------|
| AutoMerge.Core.Tests | Domain unit tests |
| AutoMerge.Logic.Tests | Application service and use case handler tests |
| AutoMerge.Infrastructure.Tests | Integration tests for external services |
| AutoMerge.UI.Tests | ViewModel logic tests |
| AutoMerge.Integration.Tests | End-to-end tests with real file I/O and mocked AI |

### 3.1 Project Dependency Rules

**Dependency Hierarchy (top to bottom):**

1. **AutoMerge.App** (top) — References all projects. Configures DI container.
2. **AutoMerge.UI** — References Application and Core.
3. **AutoMerge.Logic** — References Core only.
4. **AutoMerge.Infrastructure** — References Core and Application.
5. **AutoMerge.Core** (bottom) — References nothing. Zero external dependencies.

**Critical Rule:** Dependencies flow downward only. Lower layers NEVER reference higher layers. This ensures that Core remains pure and testable, and that Infrastructure can be swapped without affecting business logic.

---

## 4. Layer Specifications

### 4.1 AutoMerge.Core (Domain Layer)

**Purpose:** Contains the core domain models, value objects, and domain logic that represent merge conflict concepts. This layer has **zero external dependencies** (no NuGet packages except .NET BCL).

**Contains:**
- Domain entities and value objects
- Domain enums and constants
- Domain exceptions
- Pure domain logic (conflict parsing, diff computation concepts)
- Interface definitions for infrastructure services

#### 4.1.1 Domain Models

| Model | Purpose |
|-------|--------|
| **AiServiceStatus** | Represents the current AI connection state. Contains `IsAvailable` (CLI reachable), `IsAuthenticated` (valid token), `ErrorMessage` (human-readable issue), and `ActiveModel` (the model name currently in use, e.g., "GPT-5 mini"). |
| ConflictFile | Represents a file containing one or more conflicts. Holds the original content and detected conflict regions. |
| ConflictRegion | A single conflict region within a file. Contains the base, local, and remote versions of the conflicting section plus line numbers. |
| FileVersion | Enumeration: Base, Local, Remote, Merged. Used to identify which version of a file is being referenced. |
| MergeInput | The four input file paths for a merge operation (base, local, remote, output). Immutable value object. |
| MergeResolution | The resolved content and metadata including explanation text and confidence indicators. |
| ConflictAnalysis | AI analysis results including semantic descriptions of what each side changed and why they conflict. |
| ChatMessage | A message in the AI conversation. Includes role (user/assistant), content, and timestamp. |
| UserPreferences | User configuration settings: default bias, auto-analyze on load, theme, and AI model selection. Defaults to `"GPT-5 mini"` as the AI model. Available model options are loaded at runtime from `IConfigurationService.LoadAiModelOptionsAsync()` (backed by a bundled `ai-models.xml` catalog) rather than a hardcoded list. Users may also specify custom model identifiers. |
| LineChange | Represents a single line's diff status (added, removed, modified, unchanged) with line numbers. |
| AutoResolvedRegion | Marks a region in the merged content that was automatically resolved by deterministic three-way merge logic (not AI). Carries start and end line numbers. Used by the UI to render green highlighting for auto-resolved sections. |

#### 4.1.2 Core Interfaces

These interfaces define the contracts that Infrastructure must implement. They live in Core so that Application and UI can depend on them without coupling to Infrastructure.

| Interface | Contract |
|-----------|----------|
| IAiService | AI interaction: analyze conflicts, propose resolutions, refine with conversation, explain changes. `GetStatusAsync` returns `AiServiceStatus` including the active model name. |
| IFileService | File I/O: read files with encoding detection, write files preserving encoding and line endings. |
| IConfigurationService | Settings persistence: load, save, and reset user preferences. Also loads available AI model options from a bundled catalog. |
| IConflictParser | Conflict marker parsing: parse Git conflict markers, validate resolution has no remaining markers. |
| IDiffCalculator | Diff computation: calculate line-by-line differences between file versions. |

#### 4.1.3 Domain Services

Pure domain logic that doesn't fit in entities. These are stateless services with no external dependencies.

| Service | Responsibility |
|---------|---------------|
| ConflictMarkerParser | Parses Git conflict markers from text content. Identifies conflict regions and extracts base/local/remote sections. Pure string manipulation logic. |
| LineEndingDetector | Detects the line ending style of a file (CRLF, LF, or mixed). Used to preserve original line endings when writing output. |
| EncodingDetector | Detects the character encoding of a file (UTF-8, UTF-16, etc.). Used to preserve original encoding when writing output. |

---

### 4.2 AutoMerge.Logic (Application Layer)

**Purpose:** Orchestrates use cases by coordinating domain objects and infrastructure services. Contains application-specific business rules and workflows.

**Contains:**
- Use case handlers (one per user action)
- Application services (cross-cutting coordination)
- DTOs for cross-layer communication
- Event definitions for reactive updates

#### 4.2.1 Use Cases

Each use case follows a consistent pattern: a Command (input DTO), a Result (output DTO), and a Handler (orchestration logic). Each handler has a single public `ExecuteAsync` method.

| Use Case | Purpose |
|----------|--------|
| **LoadMergeSession** | Reads the four input files, parses conflict markers, creates a MergeSession, and stores it in the session manager. Entry point for the application. |
| **AutoResolveConflicts** | Performed automatically on load inside MergedResultViewModel. Deterministically resolves trivial conflicts where one side is unchanged from base (take the other side) or both sides made identical changes (take either). Produces AutoResolvedRegion list for UI highlighting. No AI involved. |
| **AnalyzeConflict** | Sends conflict context to AI service, receives structured analysis of what changed and why. Updates session with analysis. |
| **ProposeResolution** | Requests AI to propose a merged resolution. Handles streaming responses. Updates session with proposed content. |
| **RefineResolution** | Sends user's refinement message to AI, receives updated resolution while maintaining conversation context. |
| **AcceptResolution** | Validates the resolution (no conflict markers), writes to output file with correct encoding/line endings, signals success. |
| **CancelMerge** | Cleans up session state without writing any files. Signals cancellation to calling process. Also invoked automatically when the window is closed via the OS close button (X) or Alt+F4 to ensure the Git client sees exit code 1. |
| **SavePreferences** | Persists user preferences to platform-appropriate storage location. |
| **LoadPreferences** | Loads user preferences from storage, returning defaults if none exist. |
| **LoadAiModelOptions** | Loads the list of available AI model names from the bundled `ai-models.xml` catalog via `IConfigurationService.LoadAiModelOptionsAsync()`. Returns a fallback list containing only the default model if the catalog is missing or unreadable. |

#### 4.2.2 Application Services

| Service | Responsibility |
|---------|---------------|
| MergeSessionManager | Manages the active MergeSession instance. Provides access to current session state. Scoped lifetime (one per application run). |
| AutoSaveService | Handles periodic draft saving (every 30 seconds). Saves work-in-progress to temp directory (`%TEMP%/AutoMerge/draft-{sessionId}.txt`). Cleans up on normal exit. |

#### 4.2.3 Events

Events enable decoupled communication between Application and UI layers using a pub/sub pattern. This is especially important for AI streaming updates.

| Event | When Published |
|-------|---------------|
| SessionLoadedEvent | After files are loaded and session is ready |
| AnalysisStartedEvent | When AI analysis begins |
| AnalysisCompletedEvent | When AI analysis finishes (success or failure) |
| ResolutionProposedEvent | When AI returns a proposed resolution |
| AiStreamingChunkEvent | For each token received during AI streaming (enables real-time display) |
| AiErrorEvent | When an AI operation fails |
| SessionCompletedEvent | When session ends (accept or cancel) |

---

### 4.3 AutoMerge.Infrastructure (Infrastructure Layer)

**Purpose:** Implements the interfaces defined in Core. Contains all external service integrations, file I/O, and platform-specific code.

**Contains:**
- Copilot SDK integration
- File system operations
- Configuration persistence
- Platform-specific implementations

#### 4.3.1 AI Integration

The AI integration layer implements `IAiService` using the GitHub Copilot SDK (`GitHub.Copilot.SDK` NuGet package).

**Prerequisites:**
- GitHub Copilot CLI must be installed and available in PATH
- User must be authenticated via `copilot auth login` before first use
- Requires an active GitHub Copilot subscription

**Authentication Flow:**
1. On startup, `CopilotAiService.GetStatusAsync()` attempts to connect to the Copilot CLI
2. If CLI is not found, UI shows "GitHub Copilot CLI not found" with installation link
3. If CLI is found but user not authenticated, UI shows "Please authenticate with GitHub Copilot CLI: run 'copilot auth login'"
4. Once authenticated, the SDK uses the logged-in user's credentials automatically
5. No tokens are stored by AutoMerge - authentication is fully delegated to Copilot CLI

**Primary Components:**

| Component | Responsibility |
|-----------|---------------|
| CopilotAiService | Implements IAiService. Manages CopilotClient lifecycle, creates sessions with user-selected model, handles streaming responses with 60-second timeout, provides clear authentication status messages, and exposes `SetModel(string)` to change the active model at runtime. Parses AI responses by extracting labeled `RESOLVED_CONTENT` blocks. Returns the active model name in `AiServiceStatus.ActiveModel`. |
| SystemPrompts | Contains the merge agent system prompt and templates for analysis, resolution, and refinement operations. |
| MockAiService | Test double for unit testing. Provides canned responses without requiring Copilot CLI. |

**CopilotClient Configuration:**
```csharp
new CopilotClientOptions
{
    AutoStart = false,        // Manual control over lifecycle
    UseLoggedInUser = true    // Use Copilot CLI's existing authentication
}
```

**Session Configuration:**
```csharp
new SessionConfig
{
    Model = _activeModel,     // User-selected model from preferences (default: "GPT-5 mini")
    Streaming = true,          // Enable real-time streaming
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = SystemPrompts.MergeAgentSystemPrompt
    }
}
```

**Model Selection:**
- The active model defaults to `UserPreferences.Default.AiModel` ("GPT-5 mini")
- Users configure their preferred model in the Preferences dialog, which offers a list of models loaded from the bundled `ai-models.xml` catalog (GPT-5 mini, GPT-5.2-Codex, Claude Sonnet 4.5, Claude Opus 4.6, Claude Haiku 4.5) plus support for custom model identifiers
- `ProposeResolutionAsync` reads the model from the supplied `UserPreferences.AiModel` and calls `SetModel()` before creating a session
- The selected model is included in `AiServiceStatus.ActiveModel` so the UI can display it

**Prompts:**

System prompts and templates are stored in `Infrastructure/AI/Prompts/SystemPrompts.cs`:
- `MergeAgentSystemPrompt` - Configures the Copilot agent's persona as a merge conflict expert
- `AnalysisPromptTemplate` - Template for conflict analysis requests
- `ResolutionPromptTemplate` - Template for resolution proposal requests  
- `RefinementPromptTemplate` - Template for refinement requests

#### 4.3.2 File Operations

| Component | Responsibility |
|-----------|---------------|
| FileService | Implements IFileService. Handles file reading with automatic encoding detection (BOM-based: UTF-8 w/ BOM, UTF-16 LE, UTF-16 BE, fallback UTF-8), writing with encoding and line ending preservation, and binary file detection (probes first 8 KB for null bytes). Line ending normalization preserves the detected style (CRLF, LF, or Mixed). |
| DraftManager | Manages auto-save drafts in the system temp directory (`%TEMP%/AutoMerge/draft-{sessionId}.txt`). Handles periodic saves and cleanup. |

#### 4.3.3 Configuration

| Component | Responsibility |
|-----------|---------------|
| ConfigurationService | Implements IConfigurationService. On Windows, persists preferences as a JSON string in the Windows Registry (`HKCU\Software\AutoMerge\PreferencesJson`). On other platforms, persists to a JSON file via PlatformPaths. Also loads the AI model catalog from `ai-models.xml` in the application base directory. |
| PlatformPaths | Resolves platform-specific configuration directory. Returns `Environment.SpecialFolder.ApplicationData/AutoMerge` on all platforms (e.g., `%APPDATA%\AutoMerge` on Windows, `~/Library/Application Support/AutoMerge` on macOS). Used by ConfigurationService for non-Windows JSON preference storage. |

#### 4.3.4 Diff Calculation

| Component | Responsibility |
|-----------|---------------|
| DiffPlexCalculator | Implements IDiffCalculator using the DiffPlex library (`Differ` class). Computes line-by-line differences and maps DiffPlex result types to domain LineChange models internally. |

---

### 4.4 AutoMerge.UI (Presentation Layer)

**Purpose:** Contains all Avalonia UI components. Implements MVVM pattern with ViewModels coordinating between Views and Application layer.

**Contains:**
- Avalonia Views (XAML)
- ViewModels
- Value Converters
- UI-specific services
- Keyboard shortcut bindings

#### 4.4.1 Views

Views are Avalonia XAML files with minimal code-behind (only UI initialization logic).

**Main Window:**
- MainWindow — The primary application window. Hosts all panels and dialogs.

**Panels (embedded in main window):**

| Panel | Purpose |
|-------|--------|
| DiffPaneView | Displays one version of the file (used three times for base/local/remote). Read-only with syntax highlighting. Supports scroll-to-line binding for synchronized conflict navigation. Exposes ScrollOffsetX/ScrollOffsetY bindings for cross-panel scroll synchronisation. |
| MergedResultView | Displays the editable merged result. Full editor with syntax highlighting, undo/redo. Shows auto-resolved region highlighting (green) and unresolved conflict highlighting (red). Displays auto-resolved count badge. Includes conflict navigation controls (◀/▶) and revert-to-version buttons (Base/Local/Remote) in the panel header. Exposes ScrollOffsetX/ScrollOffsetY bindings for cross-panel scroll synchronisation. |
| AiChatPanelView | Collapsible panel for AI conversation. Shows message history with role-based bubble styling, streaming indicator, and input field with send button. Includes a welcome prompt with example questions when conversation is empty. |

**Dialogs:**

| Dialog | Purpose |
|--------|--------|
| PreferencesDialog | Modal dialog for editing user preferences |
| MergeInputDialog | Modal dialog for selecting merge input files when launched without CLI arguments. Uses native platform file pickers for browsing. |

**Custom Controls:**

| Control | Purpose |
|---------|--------|
| CodeEditorControl | Wrapper around AvaloniaEdit. Configures syntax highlighting, read-only mode, scroll-to-line, auto-resolved region highlighting, and diff/conflict background rendering. Exposes `ScrollOffsetX` and `ScrollOffsetY` Avalonia StyledProperties (TwoWay default binding) that synchronise with the underlying `TextArea.TextView.ScrollOffset`. Uses a `_isSyncingScroll` guard to prevent re-entry when the scroll offset is set programmatically. Hosts pluggable background renderers (see below) and a conflict marker folding strategy. Dark theme: background `#1A1A1E`, text `#E0E0E0`, caret `#00B7C3`, selection `#264F78`, font Cascadia Code / JetBrains Mono / Consolas. |
| DiffBackgroundRenderer | Background renderer inside CodeEditorControl. Colors added lines green (`#30A5D6A7`), removed lines red (`#30EF9A9A`), and modified lines amber (`#30FFE082`). Draws a 4 px colored left-edge marker strip (BeyondCompare-style) for each change type. |
| ConflictRegionBackgroundRenderer | Background renderer inside CodeEditorControl. Colors local sections green, base sections blue, and remote sections red. Draws 4 px left-edge marker strips. Supports both 2-way and diff3 conflict formats. Marker lines are rendered with dimmed gray. |
| AutoResolvedBackgroundRenderer | Background renderer inside CodeEditorControl. Draws a subtle green tint (`#204CAF50`) with a green marker strip (`#4CAF50`) for auto-resolved regions. |
| ConflictMarkerFoldingStrategy | Folding strategy inside CodeEditorControl. Creates `NewFolding` entries for each conflict marker line (`<<<<<<<`, `|||||||`, `=======`, `>>>>>>>`) and immediately collapses them so users see the section content without visual noise from marker lines. |

#### 4.4.2 ViewModels

**ViewModel Rules:**
- All ViewModels inherit from ViewModelBase (provides INotifyPropertyChanged via CommunityToolkit.Mvvm)
- Dependencies are received via constructor injection
- Commands are exposed for user actions
- ViewModels do NOT contain business logic — they delegate to Application layer use case handlers
- Design-time ViewModels provide sample data for XAML previews in the designer

| ViewModel | Responsibility |
|-----------|---------------|
| ViewModelBase | Abstract base class. Implements INotifyPropertyChanged. Provides common infrastructure. |
| MainWindowViewModel | Main window state and commands. Holds child ViewModels. Manages Accept/Cancel commands. Exposes AI connection status (`IsAiAvailable`, `AiModelName`, `AiDetailedStatus`, `IsAiSetupNeeded`, `AiSetupInstructions`) for the welcome screen status card and status bar. After file load, computes a resolution summary (`ShowResolutionSummary`, `ResolutionSummaryHeadline`, `ResolutionSummaryDetail`, `AllConflictsResolved`, `TotalOriginalConflicts`) and exposes `DismissSummaryCommand`. Orchestrates cross-panel scroll synchronisation: subscribes to `ScrollOffsetX`/`ScrollOffsetY` changes on all child ViewModels and propagates offsets to the other panels using a `_isSyncingScroll` re-entrancy guard. |
| DiffPaneViewModel | State for one diff pane. Holds text content, syntax highlighting language, line changes for gutter, scroll-to-line target for synchronized conflict navigation, and `ScrollOffsetX`/`ScrollOffsetY` observable properties for cross-panel scroll synchronisation. |
| MergedResultViewModel | State for editable result pane. Tracks dirty state, provides validation, handles undo/redo. Performs deterministic auto-resolution of trivial conflicts on load and exposes AutoResolvedRegions, AutoResolvedCount, and HasAutoResolved properties for UI highlighting. Exposes parsed ConflictRegions for cross-panel scroll synchronization. Exposes `ScrollOffsetX`/`ScrollOffsetY` observable properties for cross-panel scroll synchronisation. |
| AiChatViewModel | Chat panel state. Holds conversation history, manages streaming state, handles send command. |
| PreferencesViewModel | Preferences dialog state. Holds editable preference values including AI model selection (`AiModel`, `AiModelOptions` loaded from catalog), handles save/cancel/reset. |
| MergeInputDialogViewModel | Merge input dialog state. Holds file paths and validation state. |

#### 4.4.3 ViewModel Responsibilities

**MainWindowViewModel:**
- Holds references to child ViewModels
- Manages overall application state (loading, ready, processing, etc.)
- Handles Accept/Cancel commands
- Coordinates theme switching
- **Exposes AI connection and model state:** `IsAiAvailable`, `AiModelName` (active model), `AiDetailedStatus` (e.g., "Connected · GPT-5 mini"), `IsAiSetupNeeded`, `AiSetupInstructions` (step-by-step guidance when AI is unavailable). These are consumed by the welcome screen AI status card and the status bar.
- **Synchronises conflict navigation across panels:** When the user navigates to a conflict in the merged result (Next/Previous Conflict), finds the corresponding content in each source file and scrolls the local, base, and remote DiffPaneViewModels to the matching line.
- **Computes resolution summary:** After file load, calls `UpdateResolutionSummary()` to set headline, detail, and `AllConflictsResolved` state based on `MergedResultViewModel.AutoResolvedCount` and `TotalConflictCount`. The summary banner is visible until dismissed by the user.
- **Orchestrates cross-panel scroll synchronisation:** Subscribes to `PropertyChanged` on all four child ViewModels (Base, Local, Remote, MergedResult). When `ScrollOffsetX` or `ScrollOffsetY` changes on any panel, propagates the new offset to all other panels. A `_isSyncingScroll` flag prevents infinite re-entry.
- **Sets State on Accept/Cancel:** Updates `State` to `Saved` or `Cancelled` so the window close handler knows whether the session concluded intentionally.

**DiffPaneViewModel:**
- Holds the text content for one version
- Manages syntax highlighting language detection
- Provides line change data for gutter display
- Handles scroll synchronization events
- Exposes ScrollToLine property so the parent ViewModel can scroll the panel to a specific line during conflict navigation
- Exposes `ScrollOffsetX` and `ScrollOffsetY` observable properties (via `[ObservableProperty]`) so that MainWindowViewModel can propagate scroll offsets across panels

**MergedResultViewModel:**
- Holds the editable merged content
- Tracks dirty state (user modifications)
- Provides validation state (conflict markers present?)
- Handles undo/redo
- **Auto-resolves trivial conflicts on load:** When `SetSourceContents` is called, runs deterministic three-way merge logic to resolve conflicts where one side is unchanged from base or both sides agree. Replaces conflict markers with clean content for those regions.
- **Exposes `AutoResolvedRegions`:** Read-only list of line ranges that were auto-resolved, used by the editor for green background highlighting.
- **Exposes `AutoResolvedCount` and `HasAutoResolved`:** For toolbar and panel header badges showing how many conflicts were auto-resolved.
- **Exposes `ConflictRegions`:** The parsed conflict regions from current content, used by MainWindowViewModel to find corresponding positions in source files during conflict navigation.

**AiChatViewModel:**
- Holds conversation history
- Manages streaming state (is AI responding?)
- Handles send message command
- Provides suggested quick actions

#### 4.4.4 Converters

Value converters transform data between ViewModel properties and XAML bindings.

| Converter | Purpose |
|-----------|--------|
| BoolToVisibilityConverter | Converts boolean to Avalonia `IsVisible` binding (true/false pass-through) |
| BoolToColorConverter | Converts boolean AI availability to green (`#4CAF50` connected) or red (`#F44336` disconnected) brush for status indicators |
| BoolToAiStatusConverter | Converts boolean AI availability to human-readable status string ("AI Connected" / "AI Disconnected") |
| BoolToSetupCardColorConverter | Converts boolean AI availability to a subtle background color for the welcome screen AI status card (green tint when connected, amber tint when disconnected) |
| ChatRoleToAlignmentConverter | Converts ChatRole to HorizontalAlignment for message bubbles (User = Right, Assistant = Left) |
| ChatRoleToBackgroundConverter | Converts ChatRole to SolidColorBrush for message bubbles (User = blue `#1E3A5F`, Assistant = neutral `#2D2D32`) |
| ChatRoleToIconConverter | Converts ChatRole to emoji icon (User = 👤, Assistant = 🤖) |
| InverseBoolConverter | Inverts a boolean value (true ↔ false). Two-way converter. |
| LineChangeTypeToColorConverter | Converts LineChange type (added/removed/changed) to appropriate color (green/red/yellow) |
| PanelTitleToColorConverter | Converts panel title string to header background brush (Base = deep blue `#1E3A5F`, Local = deep green `#1E5631`, Remote = deep magenta `#5F1E3A`, Merged = deep purple `#3A1E5F`) |
| PanelTitleToDescriptionConverter | Converts panel title string to subtitle description (e.g., Base = "Common ancestor", Local = "Your changes (ours)", Remote = "Incoming changes (theirs)", Merged = "Final resolved output") |
| PanelTitleToIconConverter | Converts panel title string to emoji icon (Base = 📋, Local = 📝, Remote = 📥, Merged = ✨) |
| SessionStateToStringConverter | Converts SessionState enum to its string representation for display in the status bar |

#### 4.4.5 UI Services

UI-specific services that don't belong in lower layers.

| Service | Responsibility |
|---------|---------------|
| ThemeService | Manages dark/light theme switching. Detects system preference and applies appropriate Avalonia theme. Maps the `Theme` enum to Avalonia `ThemeVariant`. |
| KeyboardShortcutService | Registers window-level `KeyDown` handler for global keyboard shortcuts. Supports: Cmd/Ctrl+Enter (accept), Escape (cancel), Cmd/Ctrl+S (save draft), Cmd/Ctrl+Z (undo), Cmd/Ctrl+Y (redo), Ctrl+, (open preferences). Handles both `Ctrl` (Windows) and `Meta` (macOS) modifiers. |
| DialogService | Manages modal dialog display. Provides async methods to show dialogs and await results. |
| ScrollSyncService | Synchronizes scroll position across the four diff panes when user scrolls any one pane (implemented in MainWindowViewModel via `OnPaneScrollChanged` handler with `_isSyncingScroll` guard). Scroll offsets are converted to normalized 0–1 ratios to handle varying document lengths across panels. Also handles conflict-navigation-driven scroll sync: when the user navigates to a conflict in the merged result, all source panes scroll to the corresponding content region. |

---

### 4.5 AutoMerge.App (Composition Root)

**Purpose:** The executable entry point. Responsible for:
- Parsing command-line arguments
- Configuring dependency injection container
- Bootstrapping the application
- Handling application lifecycle

**Contains:**
- Program.cs (entry point)
- DI container configuration
- CLI argument parsing
- App.axaml (Avalonia application)

#### 4.5.1 Entry Point Flow

The application entry point follows this sequence:

1. **Parse CLI arguments** using custom CliParser (manual argument parsing)
   - If no arguments are provided, launch the GUI without a session and allow file selection from the app
   - Validate that required arguments are provided when arguments are present
   - Handle --help and --version immediately (exit after display)
   - Extract file paths into MergeInput value object

2. **Build DI container**
   - Register all services with appropriate lifetimes
   - Configure Copilot SDK client
   - Register ViewModels

3. **Create MergeInput** from validated arguments (if provided)
   - Verify all input files exist and are readable
   - Verify output path is writable

4. **Launch Avalonia application**
   - MainWindow is created and receives MainWindowViewModel from DI
   - ViewModel receives MergeInput and begins initialization

5. **Return exit code** based on resolution result
   - Exit code 0: Resolution was accepted and saved
   - Exit code 1: Resolution was cancelled or an error occurred

#### 4.5.2 Structure

| Component | Responsibility |
|-----------|---------------|
| Program.cs | Entry point. Minimal code that calls into startup services. |
| App.axaml / App.axaml.cs | Avalonia application definition. Handles startup, shutdown, and unhandled exceptions. Configures FluentTheme, AvaloniaEdit styles, and the application theme resource dictionary. Subscribes to SessionCompletedEvent to trigger desktop shutdown with the appropriate exit code. |
| CliParser | Command-line argument parsing using manual argument parsing (no external library). Defines all supported arguments and options. Returns a `CliParseResult` record with parsed `MergeInput`, `ShouldExit`, `ExitCode`, `WaitForGui`, and `NoGui` flags. Resolves relative paths to absolute. Falls back to local path as merged output if base is not provided. |
| ServiceRegistration | DI container configuration. Registers all services with appropriate lifetimes. Single source of truth for dependency wiring. |
| ai-models.xml | Bundled XML catalog of available AI model names (e.g., GPT-5 mini, GPT-5.2-Codex, Claude Sonnet 4.5, Claude Opus 4.6, Claude Haiku 4.5). Read by ConfigurationService at runtime. Supports adding custom models by editing the file. |

---

## 5. Cross-Cutting Concerns

### 5.1 Dependency Injection

All services are registered in ServiceRegistration using Microsoft.Extensions.DependencyInjection.

**Lifetime Guidelines:**

| Lifetime | When to Use | Examples |
|----------|-------------|----------|
| Singleton | Services that maintain state across the app lifetime | ConfigurationService, ThemeService, DialogService, KeyboardShortcutService, CopilotAiService, FileService, ConflictMarkerParser, DiffPlexCalculator, EventAggregator |
| Scoped | Services scoped to a merge session | MergeSessionManager, AutoSaveService |
| Transient | Stateless services created fresh each time | Use case handlers (9 total), ViewModels (6 total) |

**Registration Pattern:**

Core interface abstractions are mapped to their Infrastructure implementations. For example, `IAiService` is registered with `CopilotAiService` as its implementation. This allows tests to substitute mock implementations without changing consuming code.

ViewModels are registered as transient so each view gets its own instance.

### 5.2 Error Handling Strategy

**Layer-Specific Exceptions:**
- `Core`: Domain exceptions (e.g., `InvalidConflictFormatException`)
- `Infrastructure`: Wrapped external exceptions (e.g., `AiServiceException`)
- `Application`: Use case failures returned as Result types, not exceptions
- `UI`: ViewModels catch exceptions and update error state properties

**User-Facing Errors:**
- All exceptions are caught at the ViewModel level
- Errors display in a non-modal error banner
- Critical errors show a modal dialog with options

### 5.3 Logging

- Use `Microsoft.Extensions.Logging` abstractions
- Inject `ILogger<T>` into services
- Log levels: Debug (development), Information (user actions), Warning (recoverable issues), Error (failures)
- No file content logged (privacy)

### 5.4 Async/Await Patterns

**Rules:**
1. All public async methods return `Task` or `Task<T>`
2. Use `ConfigureAwait(false)` in library code (Core, Application, Infrastructure)
3. Use `ConfigureAwait(true)` or default in UI code (need UI thread context)
4. Long-running operations report progress via `IProgress<T>`
5. Support cancellation via `CancellationToken` parameters

### 5.5 Event Aggregation

For decoupled communication between components (especially for AI streaming), the Application layer publishes events that the UI layer subscribes to.

**Example Flow:**

1. User clicks "Analyze"
2. ViewModel calls AnalyzeConflictHandler.ExecuteAsync()
3. Handler calls IAiService.AnalyzeAsync()
4. CopilotAiService receives streaming chunks from Copilot SDK
5. CopilotAiService publishes AiStreamingChunkEvent for each chunk
6. AiChatViewModel (subscribed to event) updates UI with each chunk in real-time

This pattern keeps the Infrastructure layer decoupled from UI concerns while enabling real-time streaming updates.

---

### 5.6 Build and Packaging (Windows Installer)

AutoMerge uses Inno Setup for the Windows installer. Collaborators must install
Inno Setup from https://jrsoftware.org/isinfo.php to compile the installer.

**Script Location:**
- `Installer/Windows/AutoMerge.iss`

**Publish then build installer:**
1. `dotnet publish src/AutoMerge.App -c Release -r win-x64`
2. Run ISCC on the script, for example:
   `"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Installer/Windows/AutoMerge.iss`

The script expects published output in
`src/AutoMerge.App/bin/Release/net8.0/publish`.

---

## 6. Data Flow Diagrams

### 6.1 Application Startup Flow

**Trigger:** Git client runs `automerge --base B --local L --remote R --merged M` or user runs `automerge` with no arguments

**Step 1: Program.Main**
- Parse CLI arguments into MergeInput (basePath, localPath, remotePath, outputPath)
- If no arguments are provided, start the app without a MergeInput
- Build ServiceProvider (DI container)
- Resolve MainWindowViewModel and inject MergeInput when available
- Start Avalonia application

**Step 2: MainWindowViewModel.InitializeAsync()**
- Call LoadMergeSessionHandler.ExecuteAsync(mergeInput)
- Handler reads all four files via IFileService
- Handler parses conflicts via IConflictParser
- Handler creates MergeSession and stores in MergeSessionManager
- ViewModel updates DiffPaneViewModels with file content
- MergedResultViewModel.SetSourceContents triggers deterministic auto-resolution of trivial conflicts (one side unchanged from base, or both sides identical). Auto-resolved regions are highlighted in green; remaining unresolved conflicts stay marked in red.
- If auto-analyze preference is enabled, trigger initial AI analysis

**Step 2a: No-arg GUI launch**
- User clicks "Open Merge" and selects Local/Remote/Merged (Base optional)
- ViewModel creates MergeInput and calls InitializeAsync()

### 6.2 AI Resolution Flow

**Trigger:** User clicks "Get AI Help" button

**Step 1: MainWindowViewModel**
- Sets IsAnalyzing = true (shows loading indicator)
- Calls ProposeResolutionHandler.ExecuteAsync(session)

**Step 2: ProposeResolutionHandler**
- Retrieves MergeSession from MergeSessionManager
- Builds prompt with conflict context (base, local, remote content)
- Calls IAiService.ProposeResolutionAsync(context, onChunk callback)

**Step 3: CopilotAiService**
- Creates or reuses Copilot session
- Registers custom tools (analyze_conflict, propose_resolution, etc.)
- Sends prompt to Copilot SDK
- Subscribes to streaming events from SDK
- For each streaming chunk: publishes AiStreamingChunkEvent
- On completion: returns MergeResolution domain object

**Step 4: AiChatViewModel (subscribed to AiStreamingChunkEvent)**
- Receives each streaming chunk
- Appends chunk to current message display
- UI updates in real-time with "typing" effect

**Step 5: MainWindowViewModel (on completion)**
- Sets IsAnalyzing = false
- Updates MergedResultViewModel with proposed content
- User can now edit, refine via chat, or accept

### 6.3 Accept Resolution Flow

**Trigger:** User clicks "Accept" button or presses Cmd/Ctrl+Enter

**Step 1: MainWindowViewModel.AcceptCommand**
- Retrieves merged content from MergedResultViewModel
- Validates that no conflict markers remain in content
- If validation fails: show error, abort
- If valid: calls AcceptResolutionHandler.ExecuteAsync(content, outputPath)

**Step 2: AcceptResolutionHandler**
- Detects original encoding from input files (preserves encoding)
- Detects original line endings from input files (preserves line endings)
- Calls IFileService.WriteAsync(outputPath, content, encoding, lineEnding)
- Returns success result

**Step 3: MainWindowViewModel**
- Sets ExitCode = 0 (signals success to Git)
- Sets State = Saved so the window close handler knows the session was accepted
- Triggers application shutdown (SessionCompletedEvent with Success=true)

**Step 4: Program.Main**
- Returns exit code 0
- Git mergetool sees success and marks conflict as resolved in the index

### 6.4 Cancel / Window Close Flow

**Trigger:** User clicks "Cancel" button, presses Escape, or closes the window via the OS close button (X) / Alt+F4.

**Step 1: MainWindow.OnClosing (window close path only)**
- If the session is loaded and State is not already Saved or Cancelled, invokes CancelCommand.
- A `_closingHandled` flag prevents re-entry during shutdown.

**Step 2: MainWindowViewModel.Cancel**
- Sets State = Cancelled
- Calls CancelMergeHandler.Execute()

**Step 3: CancelMergeHandler**
- Sets session state to Cancelled
- Cleans up auto-save drafts
- Publishes SessionCompletedEvent(Success=false)

**Step 4: App.OnFrameworkInitializationCompleted handler**
- Receives SessionCompletedEvent with Success=false
- Calls desktop.Shutdown(1)

**Step 5: Program.Main**
- Returns exit code 1
- Git mergetool sees failure and leaves the conflict unresolved in the index

---

## 7. Interface Contracts

The following interfaces define the contracts between layers. They are defined in AutoMerge.Core and implemented in AutoMerge.Infrastructure.

### 7.1 IAiService

**Purpose:** Abstracts all AI interaction. Allows swapping Copilot SDK for mocks in tests or alternative providers in the future.

**Methods:**

| Method | Description |
|--------|-------------|
| GetStatusAsync | Checks if the AI service is available and authenticated. Returns `AiServiceStatus` with connection state, error messages, and the `ActiveModel` name (e.g., "GPT-5 mini") when connected. |
| AnalyzeConflictAsync | Analyzes a conflict and returns structured analysis. Takes a MergeSession and optional progress callback. Returns ConflictAnalysis domain object. |
| ProposeResolutionAsync | Proposes a resolution with streaming support. Takes session, user preferences (including `AiModel` for model selection), and optional streaming chunk callback. Calls `SetModel(preferences.AiModel)` before creating the session. Returns MergeResolution. |
| RefineResolutionAsync | Sends a refinement message and returns updated resolution. Maintains conversation context. Takes session, user message, and optional streaming callback. |
| ExplainChangesAsync | Explains changes in a specific line range. Takes session and line numbers. Returns explanation string. |

**All methods:**
- Are async and return Task<T>
- Accept CancellationToken for cancellation support
- May accept IProgress<string> or Action<string> for progress/streaming updates

### 7.2 IFileService

**Purpose:** Abstracts file system operations. Enables testing without real file I/O.

**Methods:**

| Method | Description |
|--------|-------------|
| ReadAsync | Reads file content with automatic encoding detection. Returns FileContent object containing content string and detected encoding. |
| WriteAsync | Writes content to file, preserving specified encoding and line endings. Takes path, content, encoding, and line ending style. |
| ExistsAsync | Checks if a file exists and is readable. Returns boolean. |
| IsBinaryAsync | Detects if a file is binary (non-text). Returns boolean. Used to show appropriate messaging for binary conflicts. |

### 7.3 IConflictParser

**Purpose:** Parses Git conflict markers from file content. Pure logic, no I/O.

**Methods:**

| Method | Description |
|--------|-------------|
| Parse | Parses Git conflict markers from content string. Returns list of ConflictRegion objects, each containing the base/local/remote sections and line numbers. Returns empty list if no conflicts found. |
| HasConflictMarkers | Validates that content has no remaining conflict markers. Returns boolean. Used to validate resolution before saving. |

**Recognized Markers:**
- `<<<<<<<` — Start of local/ours section
- `|||||||` — Start of base section (diff3 style)
- `=======` — Separator between sections
- `>>>>>>>` — End of remote/theirs section

### 7.4 IConfigurationService

**Purpose:** Persists and retrieves user preferences.

**Methods:**

| Method | Description |
|--------|-------------|
| LoadPreferencesAsync | Loads user preferences from storage. Returns default preferences if none saved. |
| SavePreferencesAsync | Saves user preferences to platform-appropriate storage location. |
| ResetPreferencesAsync | Deletes saved preferences, resetting to defaults. |
| LoadAiModelOptionsAsync | Loads available AI model names from the bundled `ai-models.xml` catalog. Returns a fallback list containing only the default model if the catalog is missing or unreadable. |

---

## 8. State Management

### 8.1 Application State

The application progresses through the following high-level states:

| State | Description | Transitions To |
|-------|-------------|----------------|
| **Startup** | Application launching, parsing arguments | Ready (on load complete) |
| **Ready** | Files loaded, waiting for user action | Analyzing (on "Get AI Help") |
| **Analyzing** | AI is processing the conflict | Ready (on complete or error) |
| **Saving** | Writing resolved content to output file | Success or Error |
| **Success** | Resolution saved, exiting with code 0 | (terminal state) |
| **Error** | An error occurred, showing error UI | Ready (on retry) |

**Cancel Path:** From any state, user can press Cancel/Escape **or close the window** to exit immediately with code 1. The window close (X) button is treated identically to Cancel — it does not write to the output file and signals failure to the Git client.

### 8.2 MergeSession State

Managed by MergeSessionManager (scoped service). The SessionState enum tracks the session lifecycle:

| State | Description |
|-------|-------------|
| Created | Session object created, files not yet loaded |
| Loading | Currently reading input files from disk |
| Ready | Files loaded successfully, ready for AI analysis or manual editing |
| Analyzing | AI is analyzing the conflict |
| ResolutionProposed | AI has proposed a resolution, shown in merged result pane |
| Refining | User is having a conversation with AI to refine the resolution |
| UserEditing | User is manually editing the merged result |
| Validated | Resolution has been validated (no conflict markers), ready to save |
| Saved | Successfully written to output file |
| Cancelled | User cancelled the merge operation |

### 8.3 ViewModel State Properties

Each ViewModel exposes state as observable properties:

**MainWindowViewModel:**
- `SessionState State` - Current session state
- `bool IsLoading` - True during initial load
- `bool IsAiBusy` - True when AI is processing
- `bool IsAiAvailable` - True when AI service is connected and authenticated
- `string AiModelName` - Active AI model name (e.g., "GPT-5 mini")
- `string AiDetailedStatus` - Human-readable AI status (e.g., "Connected · GPT-5 mini" or "Authentication required")
- `bool IsAiSetupNeeded` - True when AI is unavailable and setup instructions should be shown
- `string? AiSetupInstructions` - Step-by-step setup instructions when AI is unavailable
- `bool CanAccept` - True when resolution is valid
- `bool HasError` - True when an error occurred
- `string? ErrorMessage` - Current error message

---

## 9. Testing Strategy

### 9.1 Unit Testing

**Core Layer:** 100% coverage target
- Test conflict marker parsing with various Git formats
- Test encoding detection
- Test line ending detection
- Test domain model behavior

**Application Layer:** 90% coverage target
- Test use case handlers with mocked dependencies
- Test state transitions
- Test error handling

**Infrastructure Layer:** 80% coverage target
- Test file operations with real file system (integration)
- Test Copilot integration with mocks
- Test configuration persistence

**UI Layer:** ViewModel logic only
- Test command enable/disable logic
- Test state property changes
- Test error handling in ViewModels

### 9.2 Integration Testing (AutoMerge.Integration.Tests)

- End-to-end tests with real file I/O and mocked AI
- Full pipeline tests from file load through conflict resolution to file write
- CLI argument parsing tests
- Exit code verification tests

### 9.3 Test Doubles

**Mocks** (implement interfaces with controllable behavior):

| Mock | Purpose |
|------|--------|
| MockAiService | Returns predefined responses for AI methods with simulated streaming delay (100 ms chunks of 20 characters). Allows testing AI integration without real Copilot calls. |
| MockFileService | In-memory file system. Reads/writes from dictionary instead of disk. |
| MockConfigurationService | In-memory preferences storage. |

**Fixtures** (test data):

| Fixture | Content |
|---------|--------|
| SimpleConflict/ | Standard two-way conflict with base, local, remote, merged, and expected files |
| Diff3Conflict/ | Three-way diff3-style conflict with base section markers |
| MultipleConflicts/ | File with multiple conflict regions |
| NoConflict/ | Clean file with no conflict markers (edge case) |

---

## 10. Security Considerations

### 10.1 Data Handling

1. **File contents** are only transmitted to GitHub Copilot service
2. **No local caching** of file contents (only drafts in temp directory, deleted on close)
3. **Preferences** do not include file paths or repository information
4. **Logs** never include file contents

### 10.2 Authentication

1. Authentication is delegated entirely to Copilot CLI
2. No tokens stored by AutoMerge
3. Re-authentication prompts handled by Copilot CLI

### 10.3 Input Validation

1. All file paths validated before use
2. Path traversal attacks prevented
3. File size limits enforced (configurable, default 10MB)

---

## 11. Performance Considerations

### 11.1 Startup Performance

- Lazy initialization of Copilot SDK (don't block UI)
- Async file loading with progress indication
- UI renders immediately with loading skeleton

### 11.2 Large File Handling

- Virtualized text rendering (AvaloniaEdit handles this)
- Diff calculation runs on background thread
- AI context limited to conflict regions (not entire file) for very large files

### 11.3 Memory Management

- File contents not duplicated unnecessarily
- Copilot session disposed when merge completes
- Draft files cleaned up on normal exit

---

## 12. Extensibility Points

The architecture supports future extensions:

### 12.1 Additional AI Providers (BYOK)

`IAiService` can have alternative implementations:
- `OpenAiService` - Direct OpenAI API
- `AzureOpenAiService` - Azure-hosted models
- `OllamaService` - Local models

### 12.2 Additional Version Control Systems

Abstract the conflict parsing:
- `GitConflictParser` (current)
- `SvnConflictParser` (future)
- `MercurialConflictParser` (future)

### 12.3 Plugins for Language-Specific Analysis

Hook points for language-aware resolution:
- Semantic parsing for known languages
- AST-aware merging
- Import/using statement deduplication

---

## 13. Glossary

| Term | Definition |
|------|------------|
| **Base** | The common ancestor version of a file before divergent changes |
| **Local/Ours** | The version from the current branch being merged into |
| **Remote/Theirs** | The version from the branch being merged |
| **Merged** | The output file where the resolution is written |
| **Conflict Region** | A section of code marked by Git conflict markers |
| **Resolution** | The final merged content chosen by the user |
| **Session** | A single merge operation from launch to accept/cancel |

---

## 14. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-31 | AI | Initial specification |
| 1.1 | 2026-02-01 | AI | Updated AI Integration section with detailed Copilot SDK usage, authentication flow, and CLI prerequisites |
| 1.2 | 2026-02-07 | AI | Added: deterministic auto-resolution of trivial conflicts on load with green visual highlighting; conflict navigation now syncs all four panels (local, base, remote, merged); window close (X) treated as Cancel (exit code 1); AutoResolvedRegion model; updated data flows and ViewModel responsibilities |
| 1.3 | 2026-02-07 | AI | AI setup UX overhaul: added UserPreferences.AiModel with curated model list; CopilotAiService now uses user-selected model instead of hard-coded gpt-4.1; AiServiceStatus carries ActiveModel; welcome screen shows prominent AI status card with setup instructions; status bar shows model name; PreferencesDialog includes AI model selector (AutoCompleteBox); added BoolToSetupCardColorConverter; MainWindowViewModel exposes AiModelName, AiDetailedStatus, IsAiSetupNeeded, AiSetupInstructions |
| 1.4 | 2026-02-07 | AI | Resolution summary banner: after file load, displays dismissible banner with auto-resolved count, remaining conflicts, color legend (green/red/amber/blue/purple), and actionable guidance. Synchronized panel scrolling: CodeEditorControl exposes ScrollOffsetX/ScrollOffsetY StyledProperties; DiffPaneViewModel and MergedResultViewModel expose matching ObservableProperties; MainWindowViewModel orchestrates cross-panel scroll sync via PropertyChanged subscription with re-entrancy guard. Updated US-010, US-011, FR-UI-013, FR-UI-014. |
| 1.5 | 2026-02-07 | AI | Model catalog update: replaced hardcoded model list with bundled `ai-models.xml` catalog (GPT-5 mini, GPT-5.2-Codex, Claude Sonnet 4.5, Claude Opus 4.6, Claude Haiku 4.5); default model changed to GPT-5 mini. Configuration: Windows preferences now stored in Registry (`HKCU\Software\AutoMerge`). Added LoadAiModelOptions use case. Replaced DiffGutterControl and StreamingTextControl with background renderers (DiffBackgroundRenderer, ConflictRegionBackgroundRenderer, AutoResolvedBackgroundRenderer) and ConflictMarkerFoldingStrategy inside CodeEditorControl. Removed ConflictNavigatorView/ViewModel (navigation embedded in MergedResultView). Removed unimplemented AuthenticationDialog and ErrorDialog. Added 7 new converters (ChatRoleTo*, PanelTitleTo*, InverseBool, SessionStateToString). Updated DI registrations to match implementation. |
---
