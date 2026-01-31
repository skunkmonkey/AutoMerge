# AutoMerge Implementation Tasks

**Related Documents:**
- [AutoMerge_PRD.md](AutoMerge_PRD.md) — Product requirements and user stories
- [AutoMerge_SPEC.md](AutoMerge_SPEC.md) — Technical architecture specification

**Task Status Legend:**
- `[ ]` — Not started
- `[-]` — In progress
- `[x]` — Complete

**Instructions for AI:**
1. Before starting any task, read the PRD, SPEC, and this TASKS file
2. Mark the task as in-progress `[-]` before beginning work
3. Follow the architecture defined in SPEC exactly — do not take shortcuts
4. Create all files in the locations specified by SPEC Section 3 (Solution Structure)
5. Mark the task as complete `[x]` when finished
6. If a task references a SPEC section, re-read that section before implementing

---

## Phase 1: Project Scaffolding

> **Goal:** Create the solution structure with all projects properly configured and referencing each other according to the dependency rules in SPEC Section 3.1.

### 1.1 Create Solution and Projects

- [x] **Task 1.1.1: Create solution and project structure**
  
  Create the .NET solution with all five source projects. Reference SPEC Section 3 for the complete structure.
  
  **Actions:**
  - Create `AutoMerge.sln` in the repository root
  - Create `src/AutoMerge.Core/AutoMerge.Core.csproj` (classlib, net8.0)
  - Create `src/AutoMerge.Application/AutoMerge.Application.csproj` (classlib, net8.0)
  - Create `src/AutoMerge.Infrastructure/AutoMerge.Infrastructure.csproj` (classlib, net8.0)
  - Create `src/AutoMerge.UI/AutoMerge.UI.csproj` (classlib, net8.0)
  - Create `src/AutoMerge.App/AutoMerge.App.csproj` (exe, net8.0)
  - Add all projects to the solution
  
  **Acceptance Criteria:**
  - Solution builds with `dotnet build`
  - Each project has a placeholder class so it compiles

- [x] **Task 1.1.2: Configure project references**
  
  Set up project references according to SPEC Section 3.1 dependency rules. Dependencies flow downward only.
  
  **Actions:**
  - AutoMerge.Core: No project references (only .NET BCL)
  - AutoMerge.Application: References Core
  - AutoMerge.Infrastructure: References Core and Application
  - AutoMerge.UI: References Core and Application
  - AutoMerge.App: References all four projects
  
  **Acceptance Criteria:**
  - Solution builds successfully
  - No circular dependencies

- [x] **Task 1.1.3: Add NuGet packages**
  
  Add required NuGet packages to each project. Reference README.txt Section 3 for package list.
  
  **Actions:**
  - AutoMerge.Core: Add `Microsoft.Extensions.Logging.Abstractions`
  - AutoMerge.Application: Add `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`
  - AutoMerge.Infrastructure: Add `DiffPlex`, `GitHub.Copilot.SDK`, `Microsoft.Extensions.Logging.Abstractions`, `System.Text.Json`
  - AutoMerge.UI: Add `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `AvaloniaEdit`, `CommunityToolkit.Mvvm`
  - AutoMerge.App: Add `System.CommandLine`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Logging`
  
  **Acceptance Criteria:**
  - `dotnet restore` succeeds
  - `dotnet build` succeeds

### 1.2 Create Test Projects

- [x] **Task 1.2.1: Create test projects**
  
  Create the four test projects per SPEC Section 3.
  
  **Actions:**
  - Create `tests/AutoMerge.Core.Tests/AutoMerge.Core.Tests.csproj`
  - Create `tests/AutoMerge.Application.Tests/AutoMerge.Application.Tests.csproj`
  - Create `tests/AutoMerge.Infrastructure.Tests/AutoMerge.Infrastructure.Tests.csproj`
  - Create `tests/AutoMerge.UI.Tests/AutoMerge.UI.Tests.csproj`
  - Add xUnit, FluentAssertions, and NSubstitute packages to each
  - Add appropriate project references to each test project
  
  **Acceptance Criteria:**
  - `dotnet test` runs (with zero tests)

---

## Phase 2: Core Layer

> **Goal:** Implement the domain models, interfaces, and domain services in AutoMerge.Core. This layer has zero external dependencies. Reference SPEC Section 4.1.

### 2.1 Domain Models

- [x] **Task 2.1.1: Create enums and value objects**
  
  Create the foundational types used throughout the domain. Reference SPEC Section 4.1.1.
  
  **Actions:**
  - Create `Models/FileVersion.cs` — Enum: Base, Local, Remote, Merged
  - Create `Models/LineEnding.cs` — Enum: LF, CRLF, Mixed
  - Create `Models/LineChangeType.cs` — Enum: Unchanged, Added, Removed, Modified
  - Create `Models/SessionState.cs` — Enum with all states from SPEC Section 8.2
  - Create `Models/LineChange.cs` — Record with LineNumber, ChangeType, Content
  
  **Acceptance Criteria:**
  - All types are in the AutoMerge.Core.Models namespace
  - Types are immutable (records or enums)

- [x] **Task 2.1.2: Create MergeInput and FileContent**
  
  Create the input-related domain models.
  
  **Actions:**
  - Create `Models/MergeInput.cs` — Immutable record with BasePath, LocalPath, RemotePath, OutputPath (all strings)
  - Create `Models/FileContent.cs` — Immutable record with Content (string), Encoding (System.Text.Encoding), DetectedLineEnding (LineEnding)
  
  **Acceptance Criteria:**
  - Types are immutable records
  - MergeInput validates that paths are not null/empty in constructor

- [x] **Task 2.1.3: Create ConflictRegion and ConflictFile**
  
  Create the conflict-related domain models.
  
  **Actions:**
  - Create `Models/ConflictRegion.cs` — Record with StartLine, EndLine, BaseContent, LocalContent, RemoteContent (all content strings can be null for 3-way conflicts without base)
  - Create `Models/ConflictFile.cs` — Record with FilePath, OriginalContent, Encoding, LineEnding, and IReadOnlyList<ConflictRegion> Regions
  
  **Acceptance Criteria:**
  - ConflictRegion exposes all three versions of the conflicting section
  - ConflictFile contains the list of conflict regions

- [x] **Task 2.1.4: Create AI-related domain models**
  
  Create models for AI interaction results.
  
  **Actions:**
  - Create `Models/ChatMessage.cs` — Record with Role (enum: User, Assistant, System), Content (string), Timestamp (DateTimeOffset)
  - Create `Models/ConflictAnalysis.cs` — Record with LocalChangeDescription, RemoteChangeDescription, ConflictReason, SuggestedApproach (all strings)
  - Create `Models/MergeResolution.cs` — Record with ResolvedContent (string), Explanation (string), Confidence (double 0-1)
  
  **Acceptance Criteria:**
  - ChatMessage.Role is an enum defined within the same file
  - All types are immutable records

- [x] **Task 2.1.5: Create UserPreferences**
  
  Create the user preferences model. Reference PRD Section 5 US-009.
  
  **Actions:**
  - Create `Models/UserPreferences.cs` — Record with:
    - DefaultBias: enum (Balanced, PreferLocal, PreferRemote)
    - AutoAnalyzeOnLoad: bool
    - Theme: enum (System, Light, Dark)
    - Include static Default property returning sensible defaults
  
  **Acceptance Criteria:**
  - UserPreferences.Default returns a valid default instance
  - All preference values from PRD US-009 are represented

- [x] **Task 2.1.6: Create MergeSession aggregate root**
  
  Create the aggregate root that encapsulates a merge operation. Reference SPEC Section 4.1.1.
  
  **Actions:**
  - Create `Models/MergeSession.cs` — Class (not record, needs mutability for state) with:
    - Id (Guid)
    - MergeInput (immutable)
    - State (SessionState, mutable)
    - ConflictFile (set after loading)
    - ConflictAnalysis (set after AI analysis)
    - ProposedResolution (MergeResolution, set after AI proposal)
    - ConversationHistory (List<ChatMessage>)
    - CurrentMergedContent (string, the working content being edited)
    - Methods: SetState(), AddChatMessage(), UpdateResolution(), etc.
  
  **Acceptance Criteria:**
  - MergeSession is the only mutable model
  - State transitions are explicit via methods
  - ConversationHistory is exposed as IReadOnlyList

### 2.2 Core Interfaces

- [x] **Task 2.2.1: Create service interfaces**
  
  Create the interfaces defined in SPEC Section 4.1.2. These define the contracts that Infrastructure implements.
  
  **Actions:**
  - Create `Abstractions/IFileService.cs` with methods from SPEC Section 7.2
  - Create `Abstractions/IConflictParser.cs` with methods from SPEC Section 7.3
  - Create `Abstractions/IConfigurationService.cs` with methods from SPEC Section 7.4
  - Create `Abstractions/IDiffCalculator.cs` with method: `IReadOnlyList<LineChange> CalculateDiff(string oldText, string newText)`
  - Create `Abstractions/IAiService.cs` with methods from SPEC Section 7.1
  
  **Acceptance Criteria:**
  - All interfaces are in AutoMerge.Core.Abstractions namespace
  - All async methods return Task<T> and accept CancellationToken
  - IAiService methods accept optional streaming callbacks per SPEC Section 7.1

### 2.3 Domain Services

- [x] **Task 2.3.1: Create ConflictMarkerParser**
  
  Create the pure domain logic for parsing Git conflict markers. Reference SPEC Section 4.1.3.
  
  **Actions:**
  - Create `Services/ConflictMarkerParser.cs` implementing IConflictParser
  - Parse standard Git markers: `<<<<<<<`, `=======`, `>>>>>>>`
  - Parse diff3 markers: `|||||||` for base section
  - Return list of ConflictRegion objects with line numbers
  - Implement HasConflictMarkers() to check for remaining markers
  
  **Acceptance Criteria:**
  - Correctly parses standard 3-way conflicts
  - Correctly parses diff3-style conflicts with base section
  - Returns empty list for files with no conflict markers
  - HasConflictMarkers returns true if any markers remain

- [x] **Task 2.3.2: Create LineEndingDetector and EncodingDetector**
  
  Create the utility services for detecting file properties. Reference SPEC Section 4.1.3.
  
  **Actions:**
  - Create `Services/LineEndingDetector.cs` with static method: `LineEnding Detect(string content)`
  - Create `Services/EncodingDetector.cs` with static method: `Encoding Detect(byte[] bytes)` — check for BOM, default to UTF-8
  
  **Acceptance Criteria:**
  - LineEndingDetector correctly identifies CRLF, LF, or Mixed
  - EncodingDetector identifies UTF-8, UTF-16 LE, UTF-16 BE from BOM
  - Both are static utility classes (no state)

### 2.4 Core Tests

- [x] **Task 2.4.1: Write ConflictMarkerParser tests**
  
  Create comprehensive tests for the conflict parser.
  
  **Actions:**
  - Test parsing standard Git conflict (no base section)
  - Test parsing diff3-style conflict (with base section)
  - Test multiple conflicts in one file
  - Test file with no conflicts returns empty list
  - Test HasConflictMarkers correctly detects remaining markers
  - Add test fixture files in `tests/AutoMerge.Core.Tests/Fixtures/`
  
  **Acceptance Criteria:**
  - All tests pass
  - Edge cases covered (empty file, markers at start/end)

---

## Phase 3: Application Layer

> **Goal:** Implement use case handlers, application services, and events. Reference SPEC Section 4.2.

### 3.1 Events

- [x] **Task 3.1.1: Create event types**
  
  Create the event types for pub/sub communication. Reference SPEC Section 4.2.3.
  
  **Actions:**
  - Create `Events/SessionLoadedEvent.cs`
  - Create `Events/AnalysisStartedEvent.cs`
  - Create `Events/AnalysisCompletedEvent.cs`
  - Create `Events/ResolutionProposedEvent.cs`
  - Create `Events/AiStreamingChunkEvent.cs` with ChunkText property
  - Create `Events/AiErrorEvent.cs` with ErrorMessage property
  - Create `Events/SessionCompletedEvent.cs` with Success property
  - Create `Events/IEventAggregator.cs` interface with Publish<T> and Subscribe<T> methods
  
  **Acceptance Criteria:**
  - All events are records (immutable)
  - IEventAggregator supports generic publish/subscribe

### 3.2 Application Services

- [x] **Task 3.2.1: Create MergeSessionManager**
  
  Create the service that manages the active merge session. Reference SPEC Section 4.2.2.
  
  **Actions:**
  - Create `Services/MergeSessionManager.cs`
  - Property: CurrentSession (MergeSession, nullable)
  - Methods: CreateSession(MergeInput), ClearSession()
  - Inject IEventAggregator, publish events on state changes
  
  **Acceptance Criteria:**
  - Only one session active at a time
  - Publishes SessionLoadedEvent when session created

- [x] **Task 3.2.2: Create AutoSaveService**
  
  Create the service for periodic draft saving. Reference SPEC Section 4.2.2.
  
  **Actions:**
  - Create `Services/AutoSaveService.cs`
  - Inject IFileService for writing drafts
  - Methods: StartAutoSave(MergeSession), StopAutoSave(), SaveDraftNow()
  - Use Timer to save every 30 seconds per PRD NFR-REL-002
  - Save to temp directory with session ID in filename
  - Method: CleanupDrafts() to delete drafts for current session
  
  **Acceptance Criteria:**
  - Saves draft at configured interval
  - Cleans up drafts on explicit call
  - Thread-safe

### 3.3 Use Case Handlers

- [x] **Task 3.3.1: Create LoadMergeSession use case**
  
  Create the handler for loading files and initializing a session. Reference SPEC Section 4.2.1.
  
  **Actions:**
  - Create `UseCases/LoadMergeSession/LoadMergeSessionCommand.cs` — record with MergeInput
  - Create `UseCases/LoadMergeSession/LoadMergeSessionResult.cs` — record with Success, ErrorMessage, Session
  - Create `UseCases/LoadMergeSession/LoadMergeSessionHandler.cs`:
    - Inject IFileService, IConflictParser, MergeSessionManager, IEventAggregator
    - Read all input files via IFileService
    - Parse conflicts in merged file (or local if merged doesn't exist yet)
    - Create MergeSession via MergeSessionManager
    - Publish SessionLoadedEvent
  
  **Acceptance Criteria:**
  - Handles missing files gracefully (returns failure result)
  - Sets session state to Ready on success
  - Detects binary files and returns appropriate error

- [x] **Task 3.3.2: Create AnalyzeConflict use case**
  
  Create the handler for AI conflict analysis.
  
  **Actions:**
  - Create `UseCases/AnalyzeConflict/AnalyzeConflictCommand.cs` — empty record (uses current session)
  - Create `UseCases/AnalyzeConflict/AnalyzeConflictResult.cs` — record with Success, Analysis, ErrorMessage
  - Create `UseCases/AnalyzeConflict/AnalyzeConflictHandler.cs`:
    - Inject IAiService, MergeSessionManager, IEventAggregator
    - Publish AnalysisStartedEvent
    - Call IAiService.AnalyzeConflictAsync
    - Update session with analysis
    - Publish AnalysisCompletedEvent
  
  **Acceptance Criteria:**
  - Sets session state to Analyzing during operation
  - Handles AI errors gracefully
  - Returns to Ready state on completion

- [x] **Task 3.3.3: Create ProposeResolution use case**
  
  Create the handler for AI resolution proposals.
  
  **Actions:**
  - Create `UseCases/ProposeResolution/ProposeResolutionCommand.cs` — record with optional Preferences
  - Create `UseCases/ProposeResolution/ProposeResolutionResult.cs` — record with Success, Resolution, ErrorMessage
  - Create `UseCases/ProposeResolution/ProposeResolutionHandler.cs`:
    - Inject IAiService, MergeSessionManager, IEventAggregator, IConfigurationService
    - Call IAiService.ProposeResolutionAsync with streaming callback
    - Streaming callback publishes AiStreamingChunkEvent
    - Update session with resolution
    - Publish ResolutionProposedEvent
  
  **Acceptance Criteria:**
  - Streaming chunks are published as they arrive
  - Session updated with final resolution

- [x] **Task 3.3.4: Create RefineResolution use case**
  
  Create the handler for conversational refinement.
  
  **Actions:**
  - Create `UseCases/RefineResolution/RefineResolutionCommand.cs` — record with UserMessage (string)
  - Create `UseCases/RefineResolution/RefineResolutionResult.cs`
  - Create `UseCases/RefineResolution/RefineResolutionHandler.cs`:
    - Add user message to session conversation history
    - Call IAiService.RefineResolutionAsync
    - Add AI response to conversation history
    - Update session resolution
  
  **Acceptance Criteria:**
  - Conversation history maintained
  - Streaming supported

- [x] **Task 3.3.5: Create AcceptResolution use case**
  
  Create the handler for saving the final resolution. Reference SPEC Section 6.3.
  
  **Actions:**
  - Create `UseCases/AcceptResolution/AcceptResolutionCommand.cs` — record with FinalContent (string)
  - Create `UseCases/AcceptResolution/AcceptResolutionResult.cs` — record with Success, ErrorMessage
  - Create `UseCases/AcceptResolution/AcceptResolutionHandler.cs`:
    - Inject IFileService, IConflictParser, MergeSessionManager, AutoSaveService
    - Validate no conflict markers remain (via IConflictParser.HasConflictMarkers)
    - Get encoding and line ending from original file
    - Write via IFileService.WriteAsync
    - Call AutoSaveService.CleanupDrafts()
    - Set session state to Saved
    - Publish SessionCompletedEvent(Success: true)
  
  **Acceptance Criteria:**
  - Validation prevents saving with conflict markers
  - Preserves original encoding and line endings
  - Cleans up draft files

- [x] **Task 3.3.6: Create CancelMerge use case**
  
  Create the handler for cancelling without saving.
  
  **Actions:**
  - Create `UseCases/CancelMerge/CancelMergeHandler.cs`:
    - Inject MergeSessionManager, AutoSaveService, IEventAggregator
    - Set session state to Cancelled
    - Cleanup drafts
    - Publish SessionCompletedEvent(Success: false)
  
  **Acceptance Criteria:**
  - No files written
  - Drafts cleaned up

- [x] **Task 3.3.7: Create preferences use cases**
  
  Create handlers for loading and saving preferences.
  
  **Actions:**
  - Create `UseCases/LoadPreferences/LoadPreferencesHandler.cs` — returns UserPreferences from IConfigurationService
  - Create `UseCases/SavePreferences/SavePreferencesCommand.cs` — record with UserPreferences
  - Create `UseCases/SavePreferences/SavePreferencesHandler.cs` — saves via IConfigurationService
  
  **Acceptance Criteria:**
  - LoadPreferences returns defaults if none saved
  - SavePreferences persists to storage

---

## Phase 4: Infrastructure Layer

> **Goal:** Implement the interfaces defined in Core. Reference SPEC Section 4.3.

### 4.1 File System

- [x] **Task 4.1.1: Create FileService**
  
  Implement IFileService for file operations. Reference SPEC Section 4.3.2.
  
  **Actions:**
  - Create `FileSystem/FileService.cs` implementing IFileService
  - ReadAsync: Read bytes, detect encoding, convert to string, detect line endings
  - WriteAsync: Convert string using encoding, normalize line endings, write
  - ExistsAsync: Check File.Exists
  - IsBinaryAsync: Check for null bytes in first 8KB
  
  **Acceptance Criteria:**
  - Correctly preserves encoding (UTF-8, UTF-16)
  - Correctly preserves line endings
  - Detects binary files

- [x] **Task 4.1.2: Create DraftManager**
  
  Create the draft file management service.
  
  **Actions:**
  - Create `FileSystem/DraftManager.cs`
  - GetDraftPath(sessionId): Returns path in system temp directory
  - SaveDraft(sessionId, content): Writes draft file
  - LoadDraft(sessionId): Reads draft if exists
  - DeleteDraft(sessionId): Removes draft file
  
  **Acceptance Criteria:**
  - Uses platform-appropriate temp directory
  - Handles missing drafts gracefully

### 4.2 Configuration

- [x] **Task 4.2.1: Create ConfigurationService**
  
  Implement IConfigurationService. Reference SPEC Section 4.3.3.
  
  **Actions:**
  - Create `Configuration/PlatformPaths.cs` — static methods for getting config directory per platform
  - Create `Configuration/ConfigurationService.cs` implementing IConfigurationService
  - Store preferences as JSON in platform-appropriate location
  - Use System.Text.Json for serialization
  
  **Acceptance Criteria:**
  - Windows: `%APPDATA%\AutoMerge\preferences.json`
  - macOS: `~/Library/Application Support/AutoMerge/preferences.json`
  - Creates directory if not exists
  - Returns defaults if file missing

### 4.3 Diff Calculation

- [x] **Task 4.3.1: Create DiffPlexCalculator**
  
  Implement IDiffCalculator using DiffPlex library. Reference SPEC Section 4.3.4.
  
  **Actions:**
  - Create `Diff/DiffPlexCalculator.cs` implementing IDiffCalculator
  - Use DiffPlex.DiffBuilder.InlineDiffBuilder or SideBySideDiffBuilder
  - Map DiffPlex results to LineChange domain model
  
  **Acceptance Criteria:**
  - Correctly identifies added, removed, modified, unchanged lines
  - Returns line numbers for each change

### 4.4 AI Integration

- [x] **Task 4.4.1: Create Copilot prompt templates**
  
  Create the system prompts and templates for AI interaction.
  
  **Actions:**
  - Create `AI/Prompts/SystemPrompts.cs` — static class with:
    - MergeAgentSystemPrompt: Configures AI as merge conflict expert
    - AnalysisPromptTemplate: Template for conflict analysis
    - ResolutionPromptTemplate: Template for proposing resolution
    - RefinementPromptTemplate: Template for conversation
  
  **Acceptance Criteria:**
  - Prompts are clear and well-structured
  - Include instructions for AI to explain reasoning
  - Reference that this is Git merge conflict resolution

- [x] **Task 4.4.2: Create CopilotAiService**
  
  Implement IAiService using GitHub Copilot SDK. Reference SPEC Section 4.3.1.
  
  **Actions:**
  - Create `AI/CopilotAiService.cs` implementing IAiService
  - Inject CopilotClient from SDK
  - GetStatusAsync: Check if client is authenticated
  - AnalyzeConflictAsync: Create session, send analysis prompt, parse response
  - ProposeResolutionAsync: Send resolution prompt with streaming
  - RefineResolutionAsync: Continue conversation with context
  - ExplainChangesAsync: Ask for explanation of specific region
  - Handle streaming via SDK events and callback
  
  **Acceptance Criteria:**
  - Proper session lifecycle management
  - Streaming chunks forwarded via callback
  - Errors wrapped in AiServiceException
  - Timeout handling (60 seconds per PRD)

- [x] **Task 4.4.3: Create mock AI service for testing**
  
  Create a mock implementation for testing without Copilot.
  
  **Actions:**
  - Create `AI/MockAiService.cs` implementing IAiService
  - Return predefined responses for each method
  - Simulate streaming by chunking response with delays
  - Allow configuring responses via constructor
  
  **Acceptance Criteria:**
  - Can be used for UI development without Copilot
  - Simulates realistic delays

### 4.5 Event Aggregator

- [x] **Task 4.5.1: Create EventAggregator implementation**
  
  Implement the pub/sub event system.
  
  **Actions:**
  - Create `Events/EventAggregator.cs` implementing IEventAggregator
  - Use ConcurrentDictionary to store subscribers
  - Publish invokes all subscribers for event type
  - Subscribe returns IDisposable for unsubscription
  - Thread-safe implementation
  
  **Acceptance Criteria:**
  - Thread-safe
  - Weak references to avoid memory leaks (or explicit unsubscribe)

---

## Phase 5: UI Layer

> **Goal:** Implement ViewModels, Views, and UI services. Reference SPEC Section 4.4.

### 5.1 Base Infrastructure

- [x] **Task 5.1.1: Create ViewModelBase and design infrastructure**
  
  Set up the MVVM base infrastructure.
  
  **Actions:**
  - Create `ViewModels/ViewModelBase.cs` — inherits from ObservableObject (CommunityToolkit.Mvvm)
  - Create `Design/DesignTimeData.cs` — static class with sample data for XAML designer
  
  **Acceptance Criteria:**
  - ViewModelBase provides INotifyPropertyChanged
  - Design-time data available for previews

- [x] **Task 5.1.2: Create UI services**
  
  Create UI-specific services. Reference SPEC Section 4.4.5.
  
  **Actions:**
  - Create `Services/ThemeService.cs` — manages dark/light theme, detects system preference
  - Create `Services/DialogService.cs` — shows modal dialogs, returns Task for async await
  - Create `Services/KeyboardShortcutService.cs` — registers global shortcuts
  
  **Acceptance Criteria:**
  - ThemeService can switch themes at runtime
  - DialogService supports showing custom dialogs

- [x] **Task 5.1.3: Create value converters**
  
  Create XAML value converters. Reference SPEC Section 4.4.4.
  
  **Actions:**
  - Create `Converters/BoolToVisibilityConverter.cs`
  - Create `Converters/InverseBoolConverter.cs`
  - Create `Converters/LineChangeTypeToColorConverter.cs`
  - Create `Converters/SessionStateToStringConverter.cs`
  
  **Acceptance Criteria:**
  - All converters implement IValueConverter
  - Colors match PRD FR-UI-003 (green/red/yellow)

### 5.2 ViewModels

- [x] **Task 5.2.1: Create DiffPaneViewModel**
  
  Create the ViewModel for a single diff pane. Reference SPEC Section 4.4.2.
  
  **Actions:**
  - Create `ViewModels/DiffPaneViewModel.cs`
  - Properties: Title, Content, LineChanges, SyntaxLanguage, IsReadOnly
  - All properties observable
  - Methods: SetContent(string content, IReadOnlyList<LineChange> changes)
  
  **Acceptance Criteria:**
  - Exposes content for binding to editor
  - Exposes line changes for gutter rendering

- [x] **Task 5.2.2: Create MergedResultViewModel**
  
  Create the ViewModel for the editable merged result pane.
  
  **Actions:**
  - Create `ViewModels/MergedResultViewModel.cs`
  - Properties: Content (two-way bindable), IsDirty, HasConflictMarkers, SyntaxLanguage
  - Commands: UndoCommand, RedoCommand, RevertToBaseCommand, RevertToLocalCommand, RevertToRemoteCommand
  - Inject IConflictParser to validate content
  
  **Acceptance Criteria:**
  - IsDirty tracks unsaved changes
  - HasConflictMarkers updates on content change
  - Revert commands restore appropriate version

- [x] **Task 5.2.3: Create AiChatViewModel**
  
  Create the ViewModel for the AI chat panel.
  
  **Actions:**
  - Create `ViewModels/AiChatViewModel.cs`
  - Properties: Messages (ObservableCollection<ChatMessage>), CurrentInput, IsAiResponding, StreamingText
  - Commands: SendMessageCommand, ClearHistoryCommand
  - Subscribe to AiStreamingChunkEvent to update StreamingText
  - Inject use case handlers for refinement
  
  **Acceptance Criteria:**
  - Messages displayed in chat format
  - Streaming text appears as AI responds
  - SendMessage disabled while AI responding

- [x] **Task 5.2.4: Create MainWindowViewModel**
  
  Create the main ViewModel that orchestrates the UI. Reference SPEC Section 4.4.2.
  
  **Actions:**
  - Create `ViewModels/MainWindowViewModel.cs`
  - Child ViewModels: BasePaneViewModel, LocalPaneViewModel, RemotePaneViewModel, MergedResultViewModel, AiChatViewModel
  - Properties: State, IsLoading, IsAiBusy, CanAccept, ErrorMessage
  - Commands: AnalyzeCommand, GetAiHelpCommand, AcceptCommand, CancelCommand, OpenPreferencesCommand
  - Inject all use case handlers
  - InitializeAsync(MergeInput): Load session and populate panes
  
  **Acceptance Criteria:**
  - Orchestrates child ViewModels
  - Commands enable/disable based on state
  - AcceptCommand validates and calls use case

- [x] **Task 5.2.5: Create PreferencesViewModel**
  
  Create the ViewModel for the preferences dialog.
  
  **Actions:**
  - Create `ViewModels/PreferencesViewModel.cs`
  - Properties: All preference values as bindable properties
  - Commands: SaveCommand, CancelCommand, ResetToDefaultsCommand
  - Load preferences on construction
  
  **Acceptance Criteria:**
  - Two-way binding for preference values
  - Save persists changes

### 5.3 Views

- [x] **Task 5.3.1: Create CodeEditorControl**
  
  Create the reusable code editor control wrapping AvaloniaEdit.
  
  **Actions:**
  - Create `Controls/CodeEditorControl.axaml` and `.axaml.cs`
  - Properties: Text (bindable), IsReadOnly, SyntaxLanguage, LineChanges
  - Configure AvaloniaEdit with syntax highlighting
  - Render line change colors in gutter
  
  **Acceptance Criteria:**
  - Syntax highlighting works
  - Line changes shown in gutter with colors

- [x] **Task 5.3.2: Create DiffPaneView**
  
  Create the view for a single diff pane.
  
  **Actions:**
  - Create `Views/Panels/DiffPaneView.axaml` and `.axaml.cs`
  - Header with title (Base/Local/Remote)
  - CodeEditorControl bound to ViewModel
  - Design for read-only display
  
  **Acceptance Criteria:**
  - Displays content with syntax highlighting
  - Shows title header

- [x] **Task 5.3.3: Create MergedResultView**
  
  Create the view for the editable merged result.
  
  **Actions:**
  - Create `Views/Panels/MergedResultView.axaml` and `.axaml.cs`
  - Editable CodeEditorControl
  - Toolbar with revert buttons
  - Validation indicator (shows warning if conflict markers present)
  
  **Acceptance Criteria:**
  - Two-way binding to content
  - Visual indicator for validation state

- [x] **Task 5.3.4: Create AiChatPanelView**
  
  Create the view for the AI chat panel.
  
  **Actions:**
  - Create `Views/Panels/AiChatPanelView.axaml` and `.axaml.cs`
  - Chat message list with user/assistant styling
  - Input textbox at bottom
  - Send button
  - Loading indicator when AI responding
  - Collapsible panel design
  
  **Acceptance Criteria:**
  - Messages styled differently for user vs assistant
  - Streaming text appears in real-time

- [x] **Task 5.3.5: Create MainWindow**
  
  Create the main application window. Reference PRD Section 6.2.
  
  **Actions:**
  - Create `Views/MainWindow.axaml` and `.axaml.cs`
  - 4-pane layout: 3 diff panes on top/left, merged result on right/bottom
  - Collapsible AI chat panel (sidebar or bottom)
  - Toolbar with Accept, Cancel, Get AI Help buttons
  - Status bar with session state
  - Bind to MainWindowViewModel
  
  **Acceptance Criteria:**
  - All four panes visible
  - Responsive layout
  - Keyboard shortcuts working (Cmd/Ctrl+Enter, Escape)

- [x] **Task 5.3.6: Create PreferencesDialog**
  
  Create the preferences dialog.
  
  **Actions:**
  - Create `Views/Dialogs/PreferencesDialog.axaml` and `.axaml.cs`
  - Form layout with all preference options
  - Save/Cancel/Reset buttons
  - Modal dialog behavior
  
  **Acceptance Criteria:**
  - All preferences editable
  - Dialog returns result to caller

### 5.4 Theming

- [x] **Task 5.4.1: Configure themes**
  
  Set up dark and light themes.
  
  **Actions:**
  - Configure Avalonia.Themes.Fluent in App.axaml
  - Create custom accent colors if needed
  - Ensure ThemeService can switch between themes
  - Support system theme detection
  
  **Acceptance Criteria:**
  - Dark theme works
  - Light theme works
  - System preference detection works

---

## Phase 6: Application Composition

> **Goal:** Wire everything together in the App project. Reference SPEC Section 4.5.

### 6.1 CLI and Entry Point

- [x] **Task 6.1.1: Create CLI parser**
  
  Create command-line argument parsing. Reference PRD Section 16 (Appendix).
  
  **Actions:**
  - Create `Startup/CliParser.cs`
  - Support positional: `automerge BASE LOCAL REMOTE MERGED`
  - Support named: `--base`, `--local`, `--remote`, `--merged`
  - Support `--help`, `--version`
  - Return MergeInput or show help/version
  
  **Acceptance Criteria:**
  - All argument formats from PRD work
  - Help text matches PRD appendix
  - Returns null if --help/--version shown

- [x] **Task 6.1.2: Create dependency injection configuration**
  
  Configure all services in DI container. Reference SPEC Section 5.1.
  
  **Actions:**
  - Create `Startup/ServiceRegistration.cs`
  - Register all Core interfaces with Infrastructure implementations
  - Register all Application services and use case handlers
  - Register all ViewModels
  - Register EventAggregator as singleton
  - Configure appropriate lifetimes per SPEC
  
  **Acceptance Criteria:**
  - All services resolvable
  - Correct lifetimes (Singleton/Scoped/Transient)

- [x] **Task 6.1.3: Create application entry point**
  
  Create Program.cs and App.axaml. Reference SPEC Section 4.5.1.
  
  **Actions:**
  - Create `Program.cs`:
    - Parse CLI args
    - If --help/--version, show and exit
    - Build ServiceProvider
    - Create MergeInput
    - Launch Avalonia app with service provider
    - Return exit code from app result
  - Create `App.axaml` and `App.axaml.cs`:
    - Configure Avalonia
    - On startup, resolve MainWindowViewModel
    - Pass MergeInput to ViewModel
    - Show MainWindow
    - Handle shutdown, return exit code
  
  **Acceptance Criteria:**
  - App launches with valid arguments
  - Exit code 0 on accept, 1 on cancel
  - --help shows usage
  - --version shows version

---

## Phase 7: Integration and Testing

> **Goal:** End-to-end testing and integration verification.

### 7.1 Integration Tests

- [x] **Task 7.1.1: Create test fixtures**
  
  Create sample conflict files for testing.
  
  **Actions:**
  - Create `tests/AutoMerge.Integration.Tests/` project
  - Create `Fixtures/SimpleConflict/` with base, local, remote, expected merged
  - Create `Fixtures/Diff3Conflict/` with diff3-style conflict
  - Create `Fixtures/MultipleConflicts/` with multiple regions
  - Create `Fixtures/NoConflict/` for edge case
  
  **Acceptance Criteria:**
  - Fixtures represent realistic conflicts
  - Include expected resolutions

- [x] **Task 7.1.2: Create end-to-end tests**
  
  Test complete flows without UI.
  
  **Actions:**
  - Test: Load files → Parse conflicts → Verify ConflictFile
  - Test: Load → Mock AI resolution → Accept → Verify output file
  - Test: Load → Cancel → Verify no output written
  - Test: Validation rejects content with conflict markers
  
  **Acceptance Criteria:**
  - All flows work end-to-end
  - File I/O tested with real files

### 7.2 UI Testing

- [x] **Task 7.2.1: Create ViewModel tests**
  
  Test ViewModels with mocked dependencies.
  
  **Actions:**
  - Test MainWindowViewModel initialization
  - Test command enable/disable states
  - Test AcceptCommand flow
  - Test CancelCommand flow
  - Test error handling
  
  **Acceptance Criteria:**
  - ViewModels tested without real UI
  - All commands tested

---

## Phase 8: Polish and Documentation

> **Goal:** Final polish, error handling, and documentation.

### 8.1 Error Handling

- [x] **Task 8.1.1: Implement graceful degradation**
  
  Handle Copilot unavailability. Reference PRD NFR-REL-001.
  
  **Actions:**
  - Check Copilot status on startup
  - If unavailable, disable AI buttons with tooltip explanation
  - Allow manual-only merge without AI
  - Show reconnect option in UI
  
  **Acceptance Criteria:**
  - App usable without Copilot
  - Clear messaging about AI unavailability

- [x] **Task 8.1.2: Add comprehensive error handling**
  
  Handle all error cases gracefully.
  
  **Actions:**
  - File not found: Show error, allow retry
  - File access denied: Show error with explanation
  - AI timeout: Show retry option
  - AI error: Show error, allow manual fallback
  - Validation error: Show inline warning
  
  **Acceptance Criteria:**
  - No unhandled exceptions crash app
  - User always has recovery path

### 8.2 Keyboard Shortcuts

- [x] **Task 8.2.1: Implement all keyboard shortcuts**
  
  Add keyboard shortcuts per PRD FR-UI-008.
  
  **Actions:**
  - Cmd/Ctrl+Enter: Accept resolution
  - Escape: Cancel/close
  - Cmd/Ctrl+S: Save draft
  - Cmd/Ctrl+Z/Y: Undo/Redo in editor
  - Cmd/Ctrl+,: Open preferences
  
  **Acceptance Criteria:**
  - All shortcuts work on both Windows and macOS
  - Shortcuts shown in button tooltips

### 8.3 Final Documentation

- [x] **Task 8.3.1: Update README**
  
  Finalize documentation.
  
  **Actions:**
  - Update README.txt with final build instructions
  - Add screenshots or diagrams if helpful
  - Document any known issues
  - Add contribution guidelines if open source
  
  **Acceptance Criteria:**
  - New developer can build from README alone

---

## Task Summary

| Phase | Task Count | Description |
|-------|-----------|-------------|
| Phase 1 | 4 | Project scaffolding and setup |
| Phase 2 | 11 | Core domain layer |
| Phase 3 | 10 | Application layer |
| Phase 4 | 9 | Infrastructure layer |
| Phase 5 | 14 | UI layer |
| Phase 6 | 3 | Application composition |
| Phase 7 | 3 | Integration testing |
| Phase 8 | 4 | Polish and documentation |
| **Total** | **58** | |

---

## Notes for AI

1. **Always read SPEC before implementing** — The SPEC is the source of truth for architecture
2. **Follow dependency rules strictly** — Core has no dependencies, layers only reference downward
3. **Use correct namespaces** — AutoMerge.Core.Models, AutoMerge.Application.UseCases, etc.
4. **Make types immutable where specified** — Records for DTOs, classes only for stateful services
5. **All async methods need CancellationToken** — Per SPEC Section 5.4
6. **No business logic in ViewModels** — Delegate to use case handlers
7. **Run `dotnet build` after each task** — Verify no compile errors before marking complete
