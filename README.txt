================================================================================
                                 AUTOMERGE
                    AI-Powered Merge Conflict Resolution Tool
================================================================================

TABLE OF CONTENTS
-----------------
1. Purpose
2. How to Use AutoMerge
3. Third-Party Dependencies
4. Setting Up Dependencies in Visual Studio
5. Build and Run (CLI)
6. Testing
7. Known Issues
8. Contributing

================================================================================
1. PURPOSE
================================================================================

AutoMerge is a cross-platform desktop application that uses AI to help 
developers resolve Git merge conflicts faster and more accurately.

Key Features:
  - AI-powered conflict analysis and resolution suggestions
  - 4-pane diff view (Base, Ours, Theirs, Merged Result)
  - Interactive chat for refining AI suggestions
  - Command-line interface for Git client integration (SourceTree, Fork, etc.)
  - Cross-platform support (Windows and macOS)

AutoMerge leverages the GitHub Copilot SDK to provide intelligent merge 
assistance without requiring you to build or manage AI infrastructure. If you
have a GitHub Copilot subscription, you're ready to go.

================================================================================
2. HOW TO USE AUTOMERGE
================================================================================

STANDALONE USAGE
----------------
Launch AutoMerge and open conflict files directly:

    automerge --base file.base --local file.ours --remote file.theirs --merged file.out

Or using positional arguments:

    automerge BASE LOCAL REMOTE MERGED


GIT MERGETOOL INTEGRATION
-------------------------
Configure Git to use AutoMerge as your merge tool:

    git config --global merge.tool automerge
    git config --global mergetool.automerge.cmd 'automerge "$BASE" "$LOCAL" "$REMOTE" "$MERGED"'
    git config --global mergetool.automerge.trustExitCode true

Then when you have conflicts:

    git mergetool


SOURCETREE INTEGRATION
----------------------
1. Open SourceTree > Preferences > Diff
2. Set "Merge Tool" to "Custom"
3. Merge Command: /path/to/automerge
4. Arguments: --base "$BASE" --local "$LOCAL" --remote "$REMOTE" --merged "$MERGED"


FORK / GITKRAKEN / TOWER
------------------------
Similar to SourceTree - add AutoMerge as a custom merge tool with the same
argument pattern.


COMMAND-LINE OPTIONS
--------------------
    --base <PATH>       Path to base (common ancestor) file
    --local <PATH>      Path to local (ours) file
    --remote <PATH>     Path to remote (theirs) file
    --merged <PATH>     Path to write merged output
    --wait              Wait for GUI to close before returning (default)
    --no-gui            Headless mode (future feature)
    --help, -h          Show help
    --version, -v       Show version

EXIT CODES
----------
    0    Resolution accepted and saved successfully
    1    Resolution cancelled or error occurred

================================================================================
3. THIRD-PARTY DEPENDENCIES
================================================================================

AutoMerge relies on the following third-party libraries and tools:

RUNTIME DEPENDENCIES
--------------------

| Package               | Version  | License | Purpose                        |
|-----------------------|----------|---------|--------------------------------|
| .NET 8.0              | 8.0+     | MIT     | Application runtime            |
| Avalonia UI           | 11.x     | MIT     | Cross-platform UI framework    |
| AvaloniaEdit          | 11.x     | MIT     | Code editor with highlighting  |
| DiffPlex              | 1.7+     | MIT     | Diff computation library       |
| GitHub.Copilot.SDK    | Latest   | MIT     | AI agent orchestration         |

EXTERNAL TOOLS
--------------

| Tool                  | Required | Purpose                               |
|-----------------------|----------|---------------------------------------|
| GitHub Copilot CLI    | Yes      | AI backend (managed by Copilot SDK)   |
| Git                   | Yes      | Version control integration           |

All dependencies are FREE and open-source (MIT licensed).


NUGET PACKAGES (installed via Visual Studio)
--------------------------------------------
  - Avalonia
  - Avalonia.Desktop
  - Avalonia.Themes.Fluent
  - Avalonia.Diagnostics (debug only)
  - AvaloniaEdit
  - DiffPlex
  - GitHub.Copilot.SDK
  - CommunityToolkit.Mvvm (optional, for MVVM pattern)

================================================================================
4. SETTING UP DEPENDENCIES IN VISUAL STUDIO
================================================================================

PREREQUISITES
-------------
Before starting, ensure you have:
  [x] Visual Studio 2022 (version 17.8 or later)
  [x] .NET 8.0 SDK installed
  [x] GitHub Copilot subscription (Individual, Business, or Enterprise)
  [x] GitHub Copilot CLI installed and authenticated


STEP 1: INSTALL .NET 8.0 SDK
----------------------------
1. Download from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Run the installer
3. Verify installation:
   
       dotnet --version
   
   Should show 8.0.x or higher


STEP 2: INSTALL GITHUB COPILOT CLI
----------------------------------
1. Follow the official install guide:

    https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli

2. Common install options:

   Homebrew (macOS):
    brew install copilot-cli

   npm (all platforms, Node.js 22+):
    npm install -g @github/copilot

3. Authenticate:
   Run `copilot` and follow the prompt to use `/login`.

4. Verify it's working:

    copilot --version


STEP 3: CREATE THE PROJECT IN VISUAL STUDIO
-------------------------------------------
1. Open Visual Studio 2022
2. Click "Create a new project"
3. Search for "Avalonia" in the template search
   - If Avalonia templates are missing, see Step 3a below
4. Select "Avalonia .NET App" or "Avalonia MVVM App"
5. Name the project "AutoMerge"
6. Select .NET 8.0 as the target framework
7. Click "Create"


STEP 3a: INSTALL AVALONIA EXTENSION (if templates missing)
----------------------------------------------------------
1. In Visual Studio, go to Extensions > Manage Extensions
2. Search for "Avalonia for Visual Studio"
3. Click "Download" and restart Visual Studio
4. The Avalonia project templates will now be available


STEP 4: INSTALL NUGET PACKAGES
------------------------------
1. Right-click the project in Solution Explorer
2. Select "Manage NuGet Packages..."
3. Click the "Browse" tab
4. Search for and install each package:

   REQUIRED PACKAGES:
   
   a) Avalonia (should already be installed with template)
      Search: Avalonia
      Install: Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent
   
   b) AvaloniaEdit
      Search: AvaloniaEdit
      Install: AvaloniaEdit
   
   c) DiffPlex
      Search: DiffPlex
      Install: DiffPlex
   
   d) GitHub Copilot SDK
      Search: GitHub.Copilot.SDK
      Install: GitHub.Copilot.SDK
   
   OPTIONAL PACKAGES:
   
   e) CommunityToolkit.Mvvm
      Search: CommunityToolkit.Mvvm
      Install: CommunityToolkit.Mvvm
      (Useful for MVVM pattern with source generators)
   
   f) Avalonia.Diagnostics (Debug only)
      Search: Avalonia.Diagnostics
      Install: Avalonia.Diagnostics
      (Press F12 at runtime to open dev tools)


STEP 5: CONFIGURE PROJECT FILE
------------------------------
Your .csproj file should include these package references:

    <ItemGroup>
        <PackageReference Include="Avalonia" Version="11.*" />
        <PackageReference Include="Avalonia.Desktop" Version="11.*" />
        <PackageReference Include="Avalonia.Themes.Fluent" Version="11.*" />
        <PackageReference Include="Avalonia.Diagnostics" Version="11.*" Condition="'$(Configuration)' == 'Debug'" />
        <PackageReference Include="AvaloniaEdit" Version="11.*" />
        <PackageReference Include="DiffPlex" Version="1.7.*" />
        <PackageReference Include="GitHub.Copilot.SDK" Version="*" />
        <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
    </ItemGroup>

For cross-platform builds, ensure your project targets both Windows and macOS:

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <RuntimeIdentifiers>win-x64;win-arm64;osx-x64;osx-arm64</RuntimeIdentifiers>
    </PropertyGroup>


STEP 6: VERIFY SETUP
--------------------
1. Build the project (Ctrl+Shift+B)
2. If no errors, your environment is ready
3. Run the project (F5) to see the default Avalonia window

================================================================================
5. BUILD AND RUN (CLI)
================================================================================

From the repository root:

    dotnet restore
    dotnet build AutoMerge.sln

Run the app with sample arguments:

    dotnet run --project src/AutoMerge.App -- --base path/to/base.txt --local path/to/local.txt --remote path/to/remote.txt --merged path/to/merged.txt

Or using positional arguments:

    dotnet run --project src/AutoMerge.App -- BASE LOCAL REMOTE MERGED

For help:

    dotnet run --project src/AutoMerge.App -- --help

================================================================================
6. TESTING
================================================================================

Run all tests:

    dotnet test AutoMerge.sln

Run a single test project:

    dotnet test tests/AutoMerge.UI.Tests/AutoMerge.UI.Tests.csproj

================================================================================
7. KNOWN ISSUES
================================================================================

- Copilot integration currently defaults to a mock AI implementation for local development.
- Linux is not supported in this release (Windows and macOS only).
- Headless mode (--no-gui) is reserved for a future release.

================================================================================
8. CONTRIBUTING
================================================================================

Contributions are welcome. Please follow these guidelines:

1. Fork the repository and create a feature branch
2. Keep changes focused and add/update tests when applicable
3. Run `dotnet build` and `dotnet test` before opening a PR
4. Describe your changes clearly in the PR description


TROUBLESHOOTING
---------------

Issue: "GitHub.Copilot.SDK package not found"
Solution: The SDK is in technical preview. Check the official GitHub repo
          for the latest package name and NuGet source:
          https://github.com/github/copilot-sdk

Issue: "Avalonia templates not showing"
Solution: Install the Avalonia VS extension from Extensions > Manage Extensions

Issue: "Copilot CLI not authenticated"
Solution: Run `copilot` and follow the `/login` prompt to complete browser auth

Issue: "Build fails with runtime identifier errors"
Solution: Ensure you have the correct .NET workloads:
          dotnet workload install macos
          dotnet workload install ios (if targeting iOS in future)

Issue: "AvaloniaEdit not rendering syntax highlighting"
Solution: Ensure you've added the TextMate grammars for your target languages.
          See AvaloniaEdit documentation for grammar setup.


ADDITIONAL RESOURCES
--------------------
  - Avalonia Docs:       https://docs.avaloniaui.net/
  - AvaloniaEdit Docs:   https://github.com/AvaloniaUI/AvaloniaEdit
  - DiffPlex Docs:       https://github.com/mmanela/diffplex
  - Copilot SDK Docs:    https://github.com/github/copilot-sdk
  - .NET 8 Docs:         https://learn.microsoft.com/dotnet/

================================================================================
                              END OF README
================================================================================
